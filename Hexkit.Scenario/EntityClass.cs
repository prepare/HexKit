using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {
    #region Type Aliases

    using CompassList = ListEx<Compass>;
    using EntityImagePair = KeyValuePair<String, EntityImage>;
    using ImageStackEntryList = ListEx<ImageStackEntry>;
    using ParagraphList = ListEx<String>;
    using PointIList = ListEx<PointI>;
    using VariableModifierDictionary = SortedListEx<String, VariableModifier>;
    using VariableValueDictionary = SortedListEx<String, Int32>;

    #endregion

    /// <summary>
    /// Manages common data for all entity classes.</summary>
    /// <remarks>
    /// <b>EntityClass</b> corresponds to the complex XML type "entityClass" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.</remarks>

    public abstract class EntityClass:
        ScenarioElement, ICloneable, IMutableKeyedValue<String>, IValuable {
        #region EntityClass(String, EntityCategory)

        /// <overloads>
        /// Initializes a new instance of the <see cref="EntityClass"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityClass"/> class with the specified
        /// identifier and category.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="Id"/> property.</param>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> property.</param>
        /// <remarks>
        /// This constructor sets the <see cref="HasDropShadow"/> flag to <c>true</c> exactly if the
        /// specfied <paramref name="category"/> equals <see cref="EntityCategory.Unit"/>.</remarks>

        protected EntityClass(string id, EntityCategory category) {

            if (id != null) this._id = String.Intern(id);
            this._category = category;
            this._hasDropShadow = (category == EntityCategory.Unit);

            ImageAnimation = AnimationMode.None;
            ImageSequence = AnimationSequence.Random;
        }

        #endregion
        #region EntityClass(EntityClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityClass"/> class with property values
        /// copied from the specified instance.</summary>
        /// <param name="entity">
        /// The <see cref="EntityClass"/> instance whose property values are copied to the new
        /// instance. </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entity"/> is a null reference.</exception>
        /// <remarks><para>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="entity"/>, including a deep copy of all mutable objects that
        /// are owned by the <paramref name="entity"/>.
        /// </para><para>
        /// The properties whose values are set by <see cref="Validate"/> or by the Hexkit graphics
        /// engine are <em>not</em> copied, but left at their default values.</para></remarks>

        protected EntityClass(EntityClass entity) {
            if (entity == null)
                ThrowHelper.ThrowArgumentNullException("entity");

            this._category = entity._category;
            this._id = entity._id;
            this._name = entity._name;

            this._blocksAttack = entity._blocksAttack;
            this._hasDropShadow = entity._hasDropShadow;
            this._isTranslucent = entity._isTranslucent;
            this._isVisible = entity._isVisible;

            this._modifierRange = entity._modifierRange;
            this._resourceTransfer = entity._resourceTransfer;
            this._useDefaultPlace = entity._useDefaultPlace;
            this._valuation = entity._valuation;

            this._attributes.AddRange(entity._attributes);
            this._attributeModifiers.AddRange(entity._attributeModifiers);
            this._counters.AddRange(entity._counters);
            this._resources.AddRange(entity._resources);
            this._resourceModifiers.AddRange(entity._resourceModifiers);
            this._buildResources.AddRange(entity._buildResources);

            // create deep copy of image stack
            this._imageStack = entity._imageStack.Copy();
            this._paragraphs.AddRange(entity._paragraphs);
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly EntityCategory _category;
        private string _id, _name = "(unnamed)";
        private bool _blocksAttack = true, _useDefaultPlace,
            _hasDropShadow, _isTranslucent, _isVisible = true;

        private int _frameIndex, _modifierRange;
        private ResourceTransferMode _resourceTransfer;
        private double _valuation;

        private readonly ImageStackEntryList _imageStack = new ImageStackEntryList();
        private readonly ParagraphList _paragraphs = new ParagraphList();

        private readonly VariableValueDictionary
            _attributes = new VariableValueDictionary(),
            _counters = new VariableValueDictionary(),
            _resources = new VariableValueDictionary(),
            _buildResources = new VariableValueDictionary();

        private readonly VariableModifierDictionary
            _attributeModifiers = new VariableModifierDictionary(),
            _resourceModifiers = new VariableModifierDictionary();

        // Connections for each combined image frame
        private List<CompassList> _allConnections;

        #endregion
        #region AllTargetModes

        /// <summary>
        /// An <see cref="Array"/> comprising all values in <see cref="TargetMode"/>.</summary>
        /// <remarks>
        /// <b>AllTargetModes</b> facilitates iterating through all values of the <see
        /// cref="TargetMode"/> enumeration. This field holds the result of
        /// <c>Enum.GetValues(typeof(TargetMode))</c>.</remarks>

        public static readonly TargetMode[] AllTargetModes =
            (TargetMode[]) Enum.GetValues(typeof(TargetMode));

        #endregion
        #region Public Properties
        #region Attributes

        /// <summary>
        /// Gets a list of all initial attributes of the <see cref="EntityClass"/>.</summary>
        /// <value>
        /// A <see cref="VariableValueDictionary"/> that maps <see cref="VariableClass.Id"/> strings
        /// of <see cref="AttributeClass"/> objects to <see cref="Int32"/> values. The default is an
        /// empty collection.</value>
        /// <remarks><para>
        /// <b>Attributes</b> never returns a null reference. The collection is read-only if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of all "attribute" XML elements, with the <see
        /// cref="VariableValueDictionary.Keys"/> holding the values of the "id" attributes, and the
        /// <see cref="VariableValueDictionary.Values"/> holding the corresponding element values.
        /// </para></remarks>

        public VariableValueDictionary Attributes {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._attributes : this._attributes.AsReadOnly());
            }
        }

        #endregion
        #region AttributeModifiers

        /// <summary>
        /// Gets a list of all attribute modifiers of the <see cref="EntityClass"/>.</summary>
        /// <value>
        /// A <see cref="VariableModifierDictionary"/> that maps <see cref="VariableClass.Id"/>
        /// strings of <see cref="AttributeClass"/> objects to <see cref="VariableModifier"/>
        /// values. The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>AttributeModifiers</b> never returns a null reference. The collection is read-only if
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of all "attributeModifier" XML elements, with the <see
        /// cref="VariableModifierDictionary.Keys"/> holding the values of the "id" attributes, and
        /// the <see cref="VariableModifierDictionary.Values"/> holding the element values
        /// corresponding to each "target" attribute.</para></remarks>

        public VariableModifierDictionary AttributeModifiers {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._attributeModifiers : this._attributeModifiers.AsReadOnly());
            }
        }

        #endregion
        #region BlocksAttack

        /// <summary>
        /// Gets or sets a value indicating whether entities based on the <see cref="EntityClass"/>
        /// obstruct the line of sight for ranged attacks.</summary>
        /// <value>
        /// <c>true</c> if entities based on the <see cref="EntityClass"/> obstruct the line of
        /// sight for ranged attacks; otherwise, <c>false</c>. The default is <c>true</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>BlocksAttack</b> holds the value of the "blocksAttack" XML attribute.</remarks>

        public bool BlocksAttack {
            [DebuggerStepThrough]
            get { return this._blocksAttack; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._blocksAttack = value;
            }
        }

        #endregion
        #region BuildResources

        /// <summary>
        /// Gets a list of all resources required to build the <see cref="EntityClass"/>.</summary>
        /// <value>
        /// A <see cref="VariableValueDictionary"/> that maps <see cref="VariableClass.Id"/> strings
        /// of <see cref="ResourceClass"/> objects to <see cref="Int32"/> values. The default is an
        /// empty collection.</value>
        /// <remarks><para>
        /// <b>BuildResources</b> never returns a null reference. The collection is read-only if
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of all "buildResource" XML elements, with the <see
        /// cref="VariableValueDictionary.Keys"/> holding the values of the "id" attributes, and the
        /// <see cref="VariableValueDictionary.Values"/> holding the corresponding element values.
        /// </para></remarks>

        public VariableValueDictionary BuildResources {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._buildResources : this._buildResources.AsReadOnly());
            }
        }

        #endregion
        #region CanDestroy

        /// <summary>
        /// Gets or sets a value indicating whether entities based on the <see cref="EntityClass"/>
        /// may be destroyed by their owner.</summary>
        /// <value>
        /// Always <c>false</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set.</exception>
        /// <remarks>
        /// The <see cref="EntityClass"/> implementation of <b>CanDestroy</b> always returns
        /// <c>false</c> and cannot be set. Derived classes may return a different value, but only
        /// instances of <see cref="TerrainClass"/> may change this property.</remarks>

        public virtual bool CanDestroy {
            [DebuggerStepThrough]
            get { return false; }
            set { ThrowHelper.ThrowInvalidOperationException(Tektosyne.Strings.PropertySet); }
        }

        #endregion
        #region Category

        /// <summary>
        /// Gets the category of the <see cref="EntityClass"/>.</summary>
        /// <value>
        /// An <see cref="EntityCategory"/> value indicating the category of the <see
        /// cref="EntityClass"/>.</value>
        /// <remarks><para>
        /// <b>Category</b> never changes once the object has been constructed.
        /// </para><para>
        /// The value of this property determines the exact type of this <see cref="EntityClass"/>
        /// object: <see cref="UnitClass"/>, <see cref="TerrainClass"/>, <see cref="EffectClass"/>,
        /// or <see cref="UpgradeClass"/>.</para></remarks>

        public EntityCategory Category {
            [DebuggerStepThrough]
            get { return this._category; }
        }

        #endregion
        #region Counters

        /// <summary>
        /// Gets a list of all initial counters of the <see cref="EntityClass"/>.</summary>
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
        #region FrameCount

        /// <summary>
        /// Gets the number of image frames for the <see cref="EntityClass"/>.</summary>
        /// <value>
        /// The number of image frames in the bitmap catalog created from the <see
        /// cref="ImageStack"/>. The default is zero.</value>
        /// <remarks><para>
        /// <b>FrameCount</b> is set by successful validation to the maximum number of <see
        /// cref="EntityImage.Frames"/> defined by any <see cref="ImageStack"/> element.
        /// </para><para>
        /// A value of zero indicates that <see cref="Validate"/> has not yet been called or that
        /// validation failed. In either case, the <see cref="EntityClass"/> should be represented
        /// by an "invalid" icon which is always the first entry in the bitmap catalog.
        /// </para></remarks>

        public int FrameCount { get; private set; }

        #endregion
        #region FrameIndex

        /// <summary>
        /// Gets or sets the bitmap catalog index of the first image frame for the <see
        /// cref="EntityClass"/>.</summary>
        /// <value>
        /// The bitmap catalog index of the first image frame created from the <see
        /// cref="ImageStack"/>. The default is zero.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a negative value.</exception>
        /// <remarks><para>
        /// <b>FrameIndex</b> is intended for use by the Hexkit graphics engine, and should be
        /// ignored by other clients.
        /// </para><para>
        /// A value of zero indicates that the <see cref="EntityClass"/> should be represented by an
        /// "invalid" icon which is always the first entry in the bitmap catalog.</para></remarks>

        public int FrameIndex {
            [DebuggerStepThrough]
            get { return this._frameIndex; }
            [DebuggerStepThrough]
            set {
                if (value < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(
                        "value", value, Tektosyne.Strings.ArgumentNegative);

                this._frameIndex = value;
            }
        }

        #endregion
        #region HasDropShadow

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="ImageStack"/> should have a drop
        /// shadow effect.</summary>
        /// <value><para>
        /// <c>true</c> if a drop shadow effect is applied when the combined <see
        /// cref="ImageStack"/> is drawn to a map view; otherwise, <c>false</c>.
        /// </para><para>
        /// The default is <c>true</c> if <see cref="Category"/> equals <see
        /// cref="EntityCategory.Unit"/>; otherwise, <c>false</c>.</para></value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>HasDropShadow</b> holds the value of the "dropShadow" XML attribute.
        /// </para><para>
        /// Hexkit will apply a drop shadow effect to the combined <see cref="ImageStack"/> if
        /// <b>HasDropShadow</b> is <c>true</c>. This effect applies only to scaled map views, not
        /// to the rendering of the <see cref="EntityClass"/> in any other display control.
        /// </para></remarks>

        public bool HasDropShadow {
            [DebuggerStepThrough]
            get { return this._hasDropShadow; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._hasDropShadow = value;
            }
        }

        #endregion
        #region Id

        /// <summary>
        /// Gets or sets the identifier of the <see cref="EntityClass"/>.</summary>
        /// <value>
        /// The identifier of the <see cref="EntityClass"/>. The default is an empty string.</value>
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
        #region ImageAnimation

        /// <summary>
        /// Gets a value indicating the animation mode for the image stack that represents the <see
        /// cref="EntityClass"/>.</summary>
        /// <value>
        /// The highest <see cref="EntityImage.Animation"/> value specified by any <see
        /// cref="ImageStack"/> element. The default is <see cref="AnimationMode.None"/>.</value>
        /// <remarks>
        /// <b>ImageAnimation</b> is set by successful validation.</remarks>

        public AnimationMode ImageAnimation { get; private set; }

        #endregion
        #region ImageSequence

        /// <summary>
        /// Gets a value indicating the animation sequence for the image stack that represents the
        /// <see cref="EntityClass"/>.</summary>
        /// <value>
        /// The highest <see cref="EntityImage.Sequence"/> value specified by any <see
        /// cref="ImageStack"/> element. The default is <see cref="AnimationSequence.Random"/>.
        /// </value>
        /// <remarks>
        /// <b>ImageSequence</b> is set by successful validation.</remarks>

        public AnimationSequence ImageSequence { get; private set; }

        #endregion
        #region ImageStack

        /// <summary>
        /// Gets a list of all images representing the <see cref="EntityClass"/>.</summary>
        /// <value>
        /// A <see cref="ImageStackEntryList"/> containing all <see cref="ImageStackEntry"/>
        /// instances whose combination represents the <see cref="EntityClass"/>. The default is an
        /// empty collection.</value>
        /// <remarks><para>
        /// <b>ImageStack</b> never returns a null reference. The collection is read-only if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>. This property holds the values of all
        /// "imageStack" XML elements.
        /// </para><para>
        /// The Hexkit graphics engine overlays all <see cref="EntityImage"/> objects in the order
        /// in which they appear in the <b>ImageStack</b>. Animation requires that at least one <see
        /// cref="EntityImage"/> contains multiple <see cref="EntityImage.Frames"/>.
        /// </para></remarks>

        public ImageStackEntryList ImageStack {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._imageStack : this._imageStack.AsReadOnly());
            }
        }

        #endregion
        #region IsTranslucent

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="ImageStack"/> is translucent.
        /// </summary>
        /// <value>
        /// <c>true</c> if the combined <see cref="ImageStack"/> contains any semi-transparent
        /// pixels; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>IsTranslucent</b> holds the value of the "translucent" XML attribute.
        /// </para><para>
        /// Hexkit will always use alpha blending to render the combined <see cref="ImageStack"/> if
        /// <b>IsTranslucent</b> is <c>true</c>. This is a relatively slow method and only strictly
        /// necessary for images with semi-transparent pixels.
        /// </para><para>
        /// If <b>IsTranslucent</b> is <c>false</c>, Hexkit may assume that all pixels are either
        /// fully opaque or fully transparent, and thus render the <see cref="ImageStack"/> by
        /// simply copying the former, without taking the target pixels into consideration.
        /// </para><para>
        /// This optimization slightly degrades the visual quality of all scaled images, however,
        /// due to the translucent pixels around the edges of scaled images. Hexkit will therefore
        /// ignore the <b>IsTranslucent</b> flag and always use alpha blending unless the "Opaque
        /// Images" performance option is enabled.</para></remarks>

        public bool IsTranslucent {
            [DebuggerStepThrough]
            get { return this._isTranslucent; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._isTranslucent = value;
            }
        }

        #endregion
        #region IsVisible

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="ImageStack"/> is visible on map
        /// views.</summary>
        /// <value>
        /// <c>true</c> if the combined <see cref="ImageStack"/> is visible on map views; otherwise,
        /// <c>false</c>. The default is <c>true</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>IsVisible</b> holds the value of the "visible" XML attribute.
        /// </para><para>
        /// <b>IsVisible</b> determines whether the combined <see cref="ImageStack"/> is drawn on
        /// <em>any</em> map view, regardless of whether the active faction can observe the entity.
        /// </para><para>
        /// The <see cref="HasDropShadow"/> and <see cref="IsTranslucent"/> flags have no effect if
        /// <b>IsVisible</b> is <c>false</c> since they only apply to map views.</para></remarks>

        public bool IsVisible {
            [DebuggerStepThrough]
            get { return this._isVisible; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._isVisible = value;
            }
        }

        #endregion
        #region ModifierRange

        /// <summary>
        /// Gets or sets the map range in which to apply ranged <see cref="AttributeModifiers"/> and
        /// <see cref="ResourceModifiers"/>.</summary>
        /// <value><para>
        /// The maximum distance from the entity, in map sites, to search for eligible units for
        /// ranged <see cref="AttributeModifiers"/> and <see cref="ResourceModifiers"/>.
        /// </para><para>-or-</para><para>
        /// Zero to indicate that all placed units are eligible, regardless of distance. The default
        /// is zero.</para></value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a negative value.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>ModifierRange</b> holds the value of the "modifierRange" XML attribute.
        /// </para><para>
        /// <b>ModifierRange</b> applies to any <see cref="AttributeModifiers"/> and <see
        /// cref="ResourceModifiers"/> whose <see cref="ModifierTarget"/> equals <see
        /// cref="ModifierTarget.UnitsRanged"/> or <see cref="ModifierTarget.OwnerUnitsRanged"/>.
        /// </para><para>
        /// For <see cref="UpgradeClass"/> instances, the <b>ModifierRange</b> extends from the <see
        /// cref="FactionClass.HomeSite"/> of the owning faction.</para></remarks>

        public int ModifierRange {
            [DebuggerStepThrough]
            get { return this._modifierRange; }
            [DebuggerStepThrough]
            set {
                if (value < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(
                        "value", value, Tektosyne.Strings.ArgumentNegative);

                ApplicationInfo.CheckEditor();
                this._modifierRange = value;
            }
        }

        #endregion
        #region Name

        /// <summary>
        /// Gets or sets the display name of the <see cref="EntityClass"/>.</summary>
        /// <value>
        /// The display name of the <see cref="EntityClass"/>. The default is the literal string
        /// "(unnamed)".</value>
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
        /// cref="EntityClass"/>.</summary>
        /// <value>
        /// A <see cref="ParagraphList"/> containing a sequence of paragraphs with additional
        /// information about the <see cref="EntityClass"/>. The default is an empty collection.
        /// </value>
        /// <remarks><para>
        /// <b>Paragraphs</b> never returns null reference. The collection is read-only if <see
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
        #region PlaceSites

        /// <summary>
        /// Gets a list of all valid placement sites for entities based on the <see
        /// cref="EntityClass"/>.</summary>
        /// <value>
        /// A <see cref="PointIList"/> containing the coordinates of all map sites on which factions
        /// may place entities based on the <see cref="EntityClass"/>. The default is an empty
        /// collection.</value>
        /// <remarks><para>
        /// <b>PlaceSites</b> returns the <see cref="AreaSection.AllPlaceSites"/> collection that
        /// corresponds to the <see cref="Id"/> string of the <see cref="EntityClass"/>. This value
        /// is never a null reference. The collection is read-only if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// <b>PlaceSites</b> adds a new empty collection to the <b>AllPlaceSites</b> dictionary if
        /// no match is found. If <b>IsEditor</b> is <c>true</c>, clients may directly add or change
        /// placement sites in the returned collection.
        /// </para><para>
        /// If <see cref="UseDefaultPlace"/> is <c>true</c>, the game may define additional default
        /// placement sites for entities based on the <see cref="EntityClass"/>.</para></remarks>

        public PointIList PlaceSites {
            get {
                var placeSites = MasterSection.Instance.Areas.AllPlaceSites;

                // get existing collection, if any
                PointIList sites;
                if (!placeSites.TryGetValue(Id, out sites)) {

                    // add new collection if not found
                    sites = new PointIList();
                    placeSites[Id] = sites;
                }

                // add read-only wrapper if not editing
                return (ApplicationInfo.IsEditor ? sites : sites.AsReadOnly());
            }
        }

        #endregion
        #region Resources

        /// <summary>
        /// Gets a list of all initial resources of the <see cref="EntityClass"/>.</summary>
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
        /// Gets a list of all resource modifiers of the <see cref="EntityClass"/>.</summary>
        /// <value>
        /// A <see cref="VariableModifierDictionary"/> that maps <see cref="VariableClass.Id"/>
        /// strings of <see cref="ResourceClass"/> objects to <see cref="VariableModifier"/> values.
        /// The default is an empty collection.</value>
        /// <remarks><para>
        /// <b>ResourceModifiers</b> never returns a null reference. The collection is read-only if
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of all "resourceModifier" XML elements, with the <see
        /// cref="VariableModifierDictionary.Keys"/> holding the values of the "id" attributes, and
        /// the <see cref="VariableModifierDictionary.Values"/> holding the element values
        /// corresponding to each "target" attribute.</para></remarks>

        public VariableModifierDictionary ResourceModifiers {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._resourceModifiers : this._resourceModifiers.AsReadOnly());
            }
        }

        #endregion
        #region ResourceTransfer

        /// <summary>
        /// Gets or sets a value indicating how entities based on the <see cref="EntityClass"/>
        /// transfer <see cref="Resources"/> to other entities or factions.</summary>
        /// <value>
        /// A <see cref="ResourceTransferMode"/> value indicating how entities based on the <see
        /// cref="EntityClass"/> transfer <see cref="Resources"/> to other entities or factions. The
        /// default is <see cref="ResourceTransferMode.None"/>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>ResourceTransfer</b> holds the value of the "resourceTransfer" XML attribute.
        /// </remarks>

        public ResourceTransferMode ResourceTransfer {
            [DebuggerStepThrough]
            get { return this._resourceTransfer; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._resourceTransfer = value;
            }
        }

        #endregion
        #region UseDefaultPlace

        /// <summary>
        /// Gets or sets a value indicating whether entities based on the <see cref="EntityClass"/>
        /// may be placed on default sites.</summary>
        /// <value>
        /// <c>true</c> if entities based on the <see cref="EntityClass"/> may be placed on
        /// class-independent default sites, in addition to those defined by <see
        /// cref="PlaceSites"/>; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>UseDefaultPlace</b> holds the value of the "defaultPlace" XML attribute.</remarks>

        public bool UseDefaultPlace {
            [DebuggerStepThrough]
            get { return this._useDefaultPlace; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._useDefaultPlace = value;
            }
        }

        #endregion
        #endregion
        #region Private Methods
        #region ScaleConnections

        /// <summary>
        /// Scales the specified <see cref="Compass"/> values by the specified scaling vector.
        /// </summary>
        /// <param name="connections">
        /// The <see cref="CompassList"/> whose elements should be scaled.</param>
        /// <param name="scaling">
        /// The scaling vector to apply to all <paramref name="connections"/>.</param>
        /// <returns><para>
        /// The specified <paramref name="connections"/> if it is a null reference or an empty
        /// collection, or if neither component of the specified <paramref name="scaling"/> vector 
        /// is less than zero.
        /// </para><para>-or-</para><para>
        /// A new <see cref="CompassList"/> that contains the specified <paramref
        /// name="connections"/> mirrored across one or both axes, depending on the specified
        /// <paramref name="scaling"/> vector.</para></returns>
        /// <remarks>
        /// The specified <paramref name="connections"/> are mirrored horizontally if the <see
        /// cref="PointI.X"/> component of the specified <paramref name="scaling"/> vector is
        /// negative, and vertically if the <see cref="PointI.Y"/> component is negative.</remarks>

        private static CompassList ScaleConnections(CompassList connections, PointI scaling) {

            // return original collection if nothing to do
            if (connections == null || connections.Count == 0)
                return connections;

            // determine whether any scaling is required
            bool flipX = (scaling.X < 0f), flipY = (scaling.Y < 0f);
            if (!flipX && !flipY) return connections;

            // flip all connections on one or both axes
            var scaledConnections = new CompassList(connections.Count);
            foreach (Compass connection in connections) {

                Compass scaledConnection = connection;
                switch (connection) {

                    case Compass.North:
                        if (flipY) scaledConnection = Compass.South;
                        break;

                    case Compass.NorthEast:
                        scaledConnection = (flipX ?
                            (flipY ? Compass.SouthWest : Compass.NorthWest) : Compass.SouthEast);
                        break;

                    case Compass.East:
                        if (flipX) scaledConnection = Compass.West;
                        break;

                    case Compass.SouthEast:
                        scaledConnection = (flipX ?
                            (flipY ? Compass.NorthWest : Compass.SouthWest) : Compass.NorthEast);
                        break;

                    case Compass.South:
                        if (flipY) scaledConnection = Compass.North;
                        break;

                    case Compass.SouthWest:
                        scaledConnection = (flipX ?
                            (flipY ? Compass.NorthEast : Compass.SouthEast) : Compass.NorthWest);
                        break;

                    case Compass.West:
                        if (flipX) scaledConnection = Compass.East;
                        break;

                    case Compass.NorthWest:
                        scaledConnection = (flipX ?
                            (flipY ? Compass.SouthEast : Compass.NorthEast) : Compass.SouthWest);
                        break;
                }

                scaledConnections.Add(scaledConnection);
            }

            return scaledConnections;
        }

        #endregion
        #endregion
        #region Create

        /// <summary>
        /// Returns a new instance of the <see cref="EntityClass"/> class with the specified
        /// identifier and category.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="Id"/> property.</param>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> property. This parameter also
        /// determines the type of the returned object.</param>
        /// <returns>
        /// An instance of one of the classes derived from the <see cref="EntityClass"/> class, as
        /// determined by <paramref name="category"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="EntityCategory"/> value.
        /// </exception>
        /// <remarks><para>
        /// <b>Create</b> simply forwards the <paramref name="id"/> parameter to the constructor of
        /// one of the classes derived from the <see cref="EntityClass"/> class, as determined by
        /// the <paramref name="category"/> parameter.
        /// </para><para>
        /// Use this method or directly invoke the constructor of the appropriate derived class,
        /// whichever is more convenient.</para></remarks>

        public static EntityClass Create(string id, EntityCategory category) {
            switch (category) {

                case EntityCategory.Unit:    return new UnitClass(id);
                case EntityCategory.Terrain: return new TerrainClass(id);
                case EntityCategory.Effect:  return new EffectClass(id);
                case EntityCategory.Upgrade: return new UpgradeClass(id);

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "category", (int) category, typeof(EntityCategory));
                    return null;
            }
        }

        #endregion
        #region GetConnections

        /// <summary>
        /// Gets a list of all visual connections to neighboring map elements defined by any <see
        /// cref="ImageFrame"/> at the specified index.</summary>
        /// <param name="frameIndex">
        /// The index of the combined <see cref="EntityImage.Frames"/> elements to check.</param>
        /// <returns>
        /// A read-only <see cref="CompassList"/> containing the <see cref="Compass"/> values of any
        /// neighboring map elements to which the combined <see cref="EntityImage.Frames"/> elements
        /// at the specified <paramref name="frameIndex"/> provide a visual connection.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="frameIndex"/> is less than zero, or greater than or equal to <see
        /// cref="FrameCount"/>.</exception>
        /// <remarks><para>
        /// <b>GetConnections</b> returns a buffered <see cref="CompassList"/> that was prepared by
        /// the last call to <see cref="Validate"/>, as follows:
        /// </para><list type="number"><item>
        /// Create an empty <see cref="CompassList"/> for each possible <paramref
        /// name="frameIndex"/>, from zero to <see cref="FrameCount"/> minus one.
        /// </item><item>
        /// Iterate over all <see cref="ImageStack"/> elements, except those whose <see
        /// cref="ImageStackEntry.Image"/> is a null reference or whose <see
        /// cref="ImageStackEntry.UseUnconnected"/> flag is <c>true</c>.
        /// </item><item>
        /// For each remaining <see cref="EntityImage"/>, iterate over its <see
        /// cref="EntityImage.Frames"/> and add any <see cref="ImageFrame.Connections"/> defined by
        /// a given <see cref="ImageFrame"/> to the <see cref="CompassList"/> that was created for
        /// the same index position.
        /// </item><item>
        /// The added <see cref="ImageFrame.Connections"/> are horizontally and/or vertically
        /// mirrored if the <see cref="ImageStackEntry.ScalingVector"/> associated with the current
        /// <see cref="ImageStackEntry"/> has any negative components.
        /// </item><item>
        /// If an <see cref="EntityImage"/> defines less than <see cref="FrameCount"/> frames,
        /// repeat its <see cref="EntityImage.Frames"/> sequence until all <see cref="FrameCount"/>
        /// indices have been visited.
        /// </item></list><para>
        /// <b>GetConnections</b> is not used by Hexkit or by the default rules. A rule script may
        /// define semantics for visually connected map elements, however.</para></remarks>

        public CompassList GetConnections(int frameIndex) {

            if (frameIndex < 0 || frameIndex >= FrameCount)
                ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat("frameIndex",
                    frameIndex, Tektosyne.Strings.ArgumentLessOrGreater, 0, FrameCount - 1);

            return (this._allConnections == null ?
                CompassList.Empty : this._allConnections[frameIndex]);
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="EntityClass"/>.</summary>
        /// <returns>
        /// The value of the <see cref="Name"/> property, if it is not an empty string; otherwise,
        /// the value of the <see cref="Id"/> property.</returns>

        public override string ToString() {
            return (Name.Length == 0 ? Id : Name);
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="EntityClass"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="EntityClass"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <see cref="EntityClass"/> does not implement <b>Clone</b>. Derived classes must override
        /// this method to call the <see cref="EntityClass(EntityClass)"/> copy constructor with
        /// this instance, and then perform any additional copying operations required by the
        /// derived class.</remarks>

        public abstract object Clone();

        #endregion
        #region IMutableKeyedValue Members

        /// <summary>
        /// Gets the identifier of the <see cref="EntityClass"/>.</summary>
        /// <value>
        /// The value of the <see cref="Id"/> property.</value>

        string IKeyedValue<String>.Key {
            [DebuggerStepThrough]
            get { return Id; }
        }

        /// <summary>
        /// Sets the identifier of the <see cref="EntityClass"/>.</summary>
        /// <param name="key">
        /// The new value for the <see cref="Id"/> property.</param>
        /// <exception cref="InvalidOperationException">
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</exception>

        void IMutableKeyedValue<String>.SetKey(string key) {
            Id = key;
        }

        #endregion
        #region IValuable Members

        /// <summary>
        /// Gets or sets the valuation of the <see cref="EntityClass"/>.</summary>
        /// <value>
        /// A <see cref="Double"/> value in the standard interval [0,1], indicating the desirability
        /// of the <see cref="EntityClass"/> to computer players. Higher values indicate greater
        /// desirability. The default is zero.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is less than zero or greater than one.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Valuation</b> holds the value of the "valuation" XML attribute.</remarks>

        public double Valuation {
            [DebuggerStepThrough]
            get { return this._valuation; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();

                if (value < 0.0 || value > 1.0)
                    ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat(
                        "value", value, Tektosyne.Strings.ArgumentLessOrGreater, 0, 1);

                this._valuation = value;
            }
        }

        #endregion
        #region ScenarioElement Members
        #region ProcessIdentifier

        /// <summary>
        /// Counts, changes, or deletes all occurrences of the specified identifier in the <see
        /// cref="EntityClass"/>.</summary>
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
        /// The number of occurrences of <paramref name="oldId"/> in the <see cref="EntityClass"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oldId"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ProcessIdentifier</b> processes <see cref="EntityClass"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="Attributes"/><br/> <see cref="AttributeModifiers"/><br/> <see
        /// cref="Counters"/><br/> <see cref="Resources"/><br/> <see cref="ResourceModifiers"/><br/>
        /// <see cref="BuildResources"/></term><description>By key</description>
        /// </item><item>
        /// <term><see cref="ImageStack"/></term><description>By value</description>
        /// </item></list></remarks>

        internal override int ProcessIdentifier(string oldId, string newId) {
            int count = 0;

            // process IDs in variable tables
            count += CollectionsUtility.ProcessKey(this._attributes, oldId, newId);
            count += CollectionsUtility.ProcessKey(this._attributeModifiers, oldId, newId);
            count += CollectionsUtility.ProcessKey(this._counters, oldId, newId);
            count += CollectionsUtility.ProcessKey(this._resources, oldId, newId);
            count += CollectionsUtility.ProcessKey(this._resourceModifiers, oldId, newId);
            count += CollectionsUtility.ProcessKey(this._buildResources, oldId, newId);

            // process IDs in image stack
            foreach (ImageStackEntry entry in ImageStack)
                count += entry.ProcessIdentifier(oldId, newId);

            return count;
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="EntityClass"/>.</summary>
        /// <exception cref="XmlException">
        /// Validation failed, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Validate</b> processes <see cref="EntityClass"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="FrameCount"/></term>
        /// <description>Set to the maximum image frame count of any <see cref="ImageStack"/>
        /// element. The frame count is one where <see cref="ImageStackEntry.SingleFrame"/> is
        /// non-negative, and the number of <see cref="EntityImage.Frames"/> otherwise.
        /// </description>
        /// </item><item>
        /// <term><see cref="ImageStack"/></term>
        /// <description>Check that collection contains at least one element. Invoke <see
        /// cref="ImageStackEntry.Validate"/> on all elements.</description>
        /// </item><item>
        /// <term><see cref="ImageAnimation"/></term>
        /// <description>Set to the highest <see cref="EntityImage.Animation"/> value specified by
        /// any <see cref="ImageStack"/> element using two or more frames.</description>
        /// </item><item>
        /// <term><see cref="ImageSequence"/></term>
        /// <description>Set to the highest <see cref="EntityImage.Sequence"/> value specified by
        /// any <see cref="ImageStack"/> element using two or more frames.</description>
        /// </item><item>
        /// <term><see cref="Attributes"/><br/> <see cref="AttributeModifiers"/><br/> <see
        /// cref="Counters"/><br/> <see cref="Resources"/><br/> <see cref="ResourceModifiers"/><br/>
        /// <see cref="BuildResources"/></term><description>Check identifiers</description>
        /// </item></list><para>
        /// Checks are only performed if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// <b>Validate</b> also prepares the merged <see cref="CompassList"/> lists that are
        /// returned by <see cref="GetConnections"/>.</para></remarks>

        internal override void Validate() {

            // reset properties
            FrameCount = 0;
            this._allConnections = null;
            ImageAnimation = AnimationMode.None;
            ImageSequence = AnimationSequence.Random;

            // additional actions if not editing
            if (!ApplicationInfo.IsEditor) {
                VariableSection variables = MasterSection.Instance.Variables;

                // check variable identifiers
                variables.ValidateCollection(Attributes, VariableCategory.Attribute, "Attribute");
                variables.ValidateCollection(AttributeModifiers, VariableCategory.Attribute, "AttributeModifier");
                variables.ValidateCollection(Counters, VariableCategory.Counter, "Counter");
                variables.ValidateCollection(Resources, VariableCategory.Resource, "Resource");
                variables.ValidateCollection(ResourceModifiers, VariableCategory.Resource, "ResourceModifier");
                variables.ValidateCollection(BuildResources, VariableCategory.Resource, "BuildResource");

                // check for empty image stack
                if (ImageStack.Count == 0)
                    ThrowHelper.ThrowXmlExceptionWithFormat(Global.Strings.XmlEntityNoImage, Id);
            }

            // set references for IDs in image stack
            foreach (ImageStackEntry entry in ImageStack) {
                entry.Validate();

                // check for valid image
                EntityImage image = entry.Image.Value;
                if (image == null) continue;

                if (entry.SingleFrame >= 0) {
                    // single frame without animation
                    if (FrameCount < 1)
                        FrameCount = 1;
                } else {
                    // determine maximum frame count
                    if (image.Frames.Count > FrameCount)
                        FrameCount = image.Frames.Count;

                    // determine animation settings
                    if (image.Frames.Count >= 2) {
                        ImageAnimation = (AnimationMode) Math.Max(
                            (int) ImageAnimation, (int) image.Animation);

                        ImageSequence = (AnimationSequence) Math.Max(
                            (int) ImageSequence, (int) image.Sequence);
                    }
                }
            }

            // check if we have any frames at all
            if (FrameCount == 0) return;

            // add one Compass collection for each combined frame
            this._allConnections = new List<CompassList>(FrameCount);
            for (int i = 0; i < FrameCount; i++) {
                var mergedConnections = CompassList.Empty;

                // merge Connections for current frame in all images
                foreach (ImageStackEntry entry in ImageStack) {
                    EntityImage image = entry.Image.Value;
                    if (image == null || entry.UseUnconnected)
                        continue;

                    // use single frame if valid, else current frame
                    int frame = (entry.SingleFrame < 0 ? i : entry.SingleFrame);
                    frame %= image.Frames.Count;

                    // adapt Connections to current scaling vector
                    CompassList connections = image.Frames[frame].Connections;
                    connections = ScaleConnections(connections, entry.ScalingVector);

                    // merge any new Connections with existing ones
                    if (connections.Count > 0) {
                        if (mergedConnections.IsReadOnly)
                            mergedConnections = new CompassList(connections);
                        else {
                            foreach (Compass compass in connections)
                                if (!mergedConnections.Contains(compass))
                                    mergedConnections.Add(compass);
                        }
                    }
                }

                // add read-only Compass collection for combined frame
                this._allConnections.Add(mergedConnections.AsReadOnly());
            }
        }

        #endregion
        #endregion
        #region XmlSerializable Members
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="EntityClass"/> object using the specified
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

            XmlUtility.ReadAttributeAsBoolean(reader, "dropShadow", ref this._hasDropShadow);
            XmlUtility.ReadAttributeAsBoolean(reader, "translucent", ref this._isTranslucent);
            XmlUtility.ReadAttributeAsBoolean(reader, "visible", ref this._isVisible);

            XmlUtility.ReadAttributeAsBoolean(reader, "blocksAttack", ref this._blocksAttack);
            XmlUtility.ReadAttributeAsBoolean(reader, "defaultPlace", ref this._useDefaultPlace);
            XmlUtility.ReadAttributeAsInt32(reader, "modifierRange", ref this._modifierRange);
            XmlUtility.ReadAttributeAsEnum(reader, "resourceTransfer", ref this._resourceTransfer);
            XmlUtility.ReadAttributeAsDouble(reader, "valuation", ref this._valuation);
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="EntityClass"/> object using the specified
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
            string element;
            ImageStackEntry entry;

            switch (reader.Name) {

                case "valuation":
                    // HACK: for compatibility only
                    element = reader.ReadString();
                    this._valuation = XmlConvert.ToDouble(element);
                    return true;

                case "images":
                    // HACK: for compatibility only
                    string idRefs = reader["ids"];
                    if (idRefs == null) return true;

                    // read display parameters
                    entry = new ImageStackEntry();
                    entry.ReadXmlObsolete(reader);

                    // clone display parameters for each image ID
                    foreach (string id in idRefs.Split(null)) {
                        ImageStackEntry clone = new ImageStackEntry(entry);
                        clone.Image = new EntityImagePair(String.Intern(id), null);
                        this._imageStack.Add(clone);
                    }
                    return true;

                case ImageStackEntry.ConstXmlName:
                    entry = new ImageStackEntry();
                    entry.ReadXml(reader);
                    this._imageStack.Add(entry);
                    return true;

                case "para":
                    element = reader.ReadString();
                    this._paragraphs.Add(element.PackSpace());
                    return true;

                case "attribute":
                    VariableClass.ReadXmlValue(reader, this._attributes);
                    return true;

                case "attributeModifier":
                    VariableClass.ReadXmlValue(reader, this._attributeModifiers);
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

                case "buildResource":
                    VariableClass.ReadXmlValue(reader, this._buildResources);
                    return true;

                default: return false;
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="EntityClass"/> object that is serialized to
        /// XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {

            writer.WriteAttributeString("id", Id);
            writer.WriteAttributeString("name", Name);

            if (HasDropShadow != (Category == EntityCategory.Unit))
                writer.WriteAttributeString("dropShadow", XmlConvert.ToString(HasDropShadow));
            if (IsTranslucent)
                writer.WriteAttributeString("translucent", XmlConvert.ToString(IsTranslucent));
            if (!IsVisible)
                writer.WriteAttributeString("visible", XmlConvert.ToString(IsVisible));

            if (!BlocksAttack)
                writer.WriteAttributeString("blocksAttack", XmlConvert.ToString(BlocksAttack));
            if (ModifierRange > 0)
                writer.WriteAttributeString("modifierRange", XmlConvert.ToString(ModifierRange));
            if (UseDefaultPlace)
                writer.WriteAttributeString("defaultPlace", XmlConvert.ToString(UseDefaultPlace));
            if (ResourceTransfer != ResourceTransferMode.None)
                writer.WriteAttributeString("resourceTransfer", ResourceTransfer.ToString());
            if (Valuation > 0.0)
                writer.WriteAttributeString("valuation", XmlConvert.ToString(Valuation));
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="EntityClass"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            foreach (ImageStackEntry entry in ImageStack)
                entry.WriteXml(writer);

            Information.WriteXmlParagraphs(writer, Paragraphs);

            VariableClass.WriteXmlValues(writer, "attribute", Attributes);
            VariableClass.WriteXmlValues(writer, "attributeModifier", AttributeModifiers);
            VariableClass.WriteXmlValues(writer, "counter", Counters);
            VariableClass.WriteXmlValues(writer, "resource", Resources);
            VariableClass.WriteXmlValues(writer, "resourceModifier", ResourceModifiers);
            VariableClass.WriteXmlValues(writer, "buildResource", BuildResources);
        }

        #endregion
        #endregion
    }
}
