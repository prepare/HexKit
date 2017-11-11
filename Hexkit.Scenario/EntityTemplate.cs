using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {
    #region Type Aliases

    using VariableModifierDictionary = SortedListEx<String, VariableModifier>;
    using VariableValueDictionary = SortedListEx<String, Int32>;

    #endregion

    /// <summary>
    /// Provides a template for a game entity.</summary>
    /// <remarks><para>
    /// <b>EntityTemplate</b> allows scenario designers to define entities with individual names, 
    /// image frame indices, and variable values, rather than relying on the default values that are
    /// generated when an <see cref="EntityClass"/> is instantiated.
    /// </para><para>
    /// <b>EntityTemplate</b> corresponds to the complex XML type "entity" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.</para></remarks>

    public sealed class EntityTemplate: ScenarioElement, ICloneable, IEquatable<EntityTemplate> {
        #region EntityTemplate(EntityCategory)

        /// <overloads>
        /// Initializes a new instance of the <see cref="EntityTemplate"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityTemplate"/> class with the specified
        /// <see cref="EntityCategory"/>.</summary>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> property.</param>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="EntityCategory"/> value.
        /// </exception>

        public EntityTemplate(EntityCategory category) {

            if (category < EntityCategory.Unit || category > EntityCategory.Upgrade)
                ThrowHelper.ThrowInvalidEnumArgumentException(
                    "category", (int) category, typeof(EntityCategory));

            this._category = category;
        }

        #endregion
        #region EntityTemplate(EntityClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityTemplate"/> class based on the
        /// specified <see cref="Scenario.EntityClass"/>.</summary>
        /// <param name="entityClass">
        /// The <see cref="Scenario.EntityClass"/> providing the initial values for the <see
        /// cref="Category"/> and <see cref="EntityClass"/> properties.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entityClass"/> is a null reference.</exception>

        public EntityTemplate(EntityClass entityClass): this(entityClass.Category) {
            this._entityClass = entityClass.Id;
        }

        #endregion
        #region EntityTemplate(EntityClass, Int32, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityTemplate"/> class based on the
        /// specified <see cref="Scenario.EntityClass"/>, and with the specified frame offset and
        /// name.</summary>
        /// <param name="entityClass">
        /// The <see cref="Scenario.EntityClass"/> providing the initial values for the <see
        /// cref="Category"/> and <see cref="EntityClass"/> properties.</param>
        /// <param name="frameOffset">
        /// The initial value for the <see cref="FrameOffset"/> property.</param>
        /// <param name="name">
        /// The initial value for the <see cref="Name"/> property. This argument may be a null
        /// reference.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entityClass"/> is a null reference.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="frameOffset"/> is less than zero.</exception>

        public EntityTemplate(EntityClass entityClass, int frameOffset, string name):
            this(entityClass) {

            if (frameOffset < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "frameOffset", frameOffset, Tektosyne.Strings.ArgumentNegative);

            this._frameOffset = frameOffset;
            this._name = name;
        }

        #endregion
        #region EntityTemplate(EntityTemplate)

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityTemplate"/> class with property
        /// values copied from the specified instance.</summary>
        /// <param name="template">
        /// The <see cref="EntityTemplate"/> instance whose property values are copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="template"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="template"/>, including a deep copy of all mutable objects that
        /// are owned by the <paramref name="template"/>.</remarks>

        public EntityTemplate(EntityTemplate template) {
            if (template == null)
                ThrowHelper.ThrowArgumentNullException("template");

            this._category = template._category;
            this._entityClass = template._entityClass;
            this._name = template._name;
            this._frameOffset = template._frameOffset;
            this._isVisible = template._isVisible;
            this._useRandomFrame = template._useRandomFrame;

            this._attributes.AddRange(template._attributes);
            this._attributeModifiers.AddRange(template._attributeModifiers);
            this._counters.AddRange(template._counters);
            this._resources.AddRange(template._resources);
            this._resourceModifiers.AddRange(template._resourceModifiers);
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly EntityCategory _category;
        private string _entityClass, _name;
        private int _frameOffset;
        private bool? _isVisible;
        private bool _useRandomFrame;

        private readonly VariableValueDictionary
            _attributes = new VariableValueDictionary(),
            _counters = new VariableValueDictionary(),
            _resources = new VariableValueDictionary();

        private readonly VariableModifierDictionary
            _attributeModifiers = new VariableModifierDictionary(),
            _resourceModifiers = new VariableModifierDictionary();

        #endregion
        #region Public Properties
        #region Attributes

        /// <summary>
        /// Gets a list of all initial attributes of the <see cref="EntityTemplate"/> that override
        /// the matching <see cref="EntityClass"/> values.</summary>
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
        /// Gets a list of all attribute modifiers of the <see cref="EntityTemplate"/> that override
        /// the matching <see cref="EntityClass"/> values.</summary>
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
        #region Category

        /// <summary>
        /// Gets the category of the <see cref="EntityClass"/> that is instantiated by the <see
        /// cref="EntityTemplate"/>.</summary>
        /// <value>
        /// An <see cref="EntityCategory"/> value indicating the <see
        /// cref="Scenario.EntityClass.Category"/> of the instantiated <see cref="EntityClass"/>.
        /// </value>
        /// <remarks>
        /// <b>Category</b> never changes once the object has been constructed.</remarks>

        public EntityCategory Category {
            [DebuggerStepThrough]
            get { return this._category; }
        }

        #endregion
        #region Counters

        /// <summary>
        /// Gets a list of all initial counters of the <see cref="EntityTemplate"/> that override
        /// the matching <see cref="EntityClass"/> values.</summary>
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
        #region EntityClass

        /// <summary>
        /// Gets or sets the identifier of the <see cref="Scenario.EntityClass"/> that is
        /// instantiated by the <see cref="EntityTemplate"/>.</summary>
        /// <value>
        /// The <see cref="Scenario.EntityClass.Id"/> string of the <see
        /// cref="Scenario.EntityClass"/> that is instantiated by the <see cref="EntityTemplate"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>EntityClass</b> returns an empty string when set to a null reference. This property
        /// holds the value of the "id" XML attribute which must refer to an "entityClass" element.
        /// </remarks>

        public string EntityClass {
            [DebuggerStepThrough]
            get { return this._entityClass ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                if (value != null) value = String.Intern(value);
                this._entityClass = value;
            }
        }

        #endregion
        #region FrameOffset

        /// <summary>
        /// Gets or sets the index offset of the <see cref="ImageFrame"/> that represents the <see
        /// cref="EntityTemplate"/>.</summary>
        /// <value>
        /// The offset that is added to the <see cref="Scenario.EntityClass.FrameIndex"/> of the 
        /// instantiated <see cref="EntityClass"/> to obtain the bitmap catalog index of the image
        /// frame that represents this <see cref="EntityTemplate"/>. The default is zero.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a negative value.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>FrameOffset</b> holds the value of the "frame" XML attribute. This property allows
        /// clients to select a specific image frame for the display of <see cref="EntityTemplate"/>
        /// objects that are not represented by animated images.</remarks>

        public int FrameOffset {
            [DebuggerStepThrough]
            get { return this._frameOffset; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();

                if (value < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(
                        "value", value, Tektosyne.Strings.ArgumentNegative);

                this._frameOffset = value;
            }
        }

        #endregion
        #region IsModified

        /// <summary>
        /// Gets a value indicating whether the <see cref="EntityTemplate"/> modifies the default
        /// properties of the underlying <see cref="EntityClass"/>.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="EntityTemplate"/> modifies the default properties of the
        /// underlying <see cref="EntityClass"/>; otherwise, <c>false</c>.</value>
        /// <remarks><para>
        /// <b>IsModified</b> determines its result as follows:
        /// </para><list type="number"><item>
        /// <c>true</c> if <see cref="FrameOffset"/> is positive, <see cref="IsVisible"/> is not a
        /// null reference, <see cref="UseRandomFrame"/> is <c>true</c>, or <see cref="Name"/> is
        /// not an empty string.
        /// </item><item>
        /// Otherwise, <c>false</c> if all variable collections are empty.
        /// </item><item>
        /// Otherwise, <c>true</c> if the current <see cref="EntitySection"/> does not contain an
        /// <see cref="Scenario.EntityClass"/> object that matches the associated <see
        /// cref="Category"/> and <see cref="EntityClass"/> identifier.
        /// </item><item>
        /// Otherwise, <c>true</c> exactly if any variable collection contains any key-and-value
        /// pairs that are not found in the corresponding variable collection of the underlying <see
        /// cref="EntityClass"/>.</item></list></remarks>

        public bool IsModified {
            get {
                if (FrameOffset > 0 || IsVisible != null || UseRandomFrame || Name.Length > 0)
                    return true;

                if (Attributes.Count == 0 && AttributeModifiers.Count == 0 &&
                    Counters.Count == 0 && Resources.Count == 0 && ResourceModifiers.Count == 0)
                    return false;

                EntityClass entityClass = MasterSection.Instance.Entities.GetEntity(this);
                if (entityClass == null) return true;

                return (AreVariablesModified(entityClass.Attributes, Attributes) ||
                    AreVariablesModified(entityClass.Counters, Counters) ||
                    AreVariablesModified(entityClass.Resources, Resources) ||
                    AreVariablesModified(entityClass.AttributeModifiers, AttributeModifiers) ||
                    AreVariablesModified(entityClass.ResourceModifiers, ResourceModifiers));
            }
        }

        #endregion
        #region IsVisible

        /// <summary>
        /// Gets or sets a value that overrides the <see cref="Scenario.EntityClass.IsVisible"/>
        /// flag of the underlying <see cref="EntityClass"/>.</summary>
        /// <value>
        /// <c>true</c> if the combined <see cref="Scenario.EntityClass.ImageStack"/> of the
        /// instantiated <see cref="EntityClass"/> is visible on map views; <c>false</c> if the <see
        /// cref="Scenario.EntityClass.ImageStack"/> is hidden; or a null reference to use the <see
        /// cref="Scenario.EntityClass.IsVisible"/> flag of the underlying <see
        /// cref="EntityClass"/>. The default is a null reference.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>IsVisible</b> holds the value of the "visible" XML attribute, if present; otherwise,
        /// a null reference.
        /// </para><para>
        /// Please refer to <see cref="Scenario.EntityClass.IsVisible"/> for further details.
        /// </para></remarks>

        public bool? IsVisible {
            [DebuggerStepThrough]
            get { return this._isVisible; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._isVisible = value;
            }
        }

        #endregion
        #region Name

        /// <summary>
        /// Gets or sets the display name of the <see cref="EntityTemplate"/>.</summary>
        /// <value>
        /// The display name of the <see cref="EntityTemplate"/>. The default is an empty string.
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
        #region Resources

        /// <summary>
        /// Gets a list of all initial resources of the <see cref="EntityTemplate"/> that override
        /// the matching <see cref="EntityClass"/> values.</summary>
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
        /// Gets a list of all resource modifiers of the <see cref="EntityTemplate"/> that override
        /// the matching <see cref="EntityClass"/> values.</summary>
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
        #region UseRandomFrame

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="EntityTemplate"/> is represented
        /// by a randomly selected <see cref="ImageFrame"/>.</summary>
        /// <value>
        /// <c>true</c> if Hexkit should use a random number between zero and one less than <see
        /// cref="Scenario.EntityClass.FrameCount"/> instead of the specified <see
        /// cref="FrameOffset"/>; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>UseRandomFrame</b> holds the value of the "randomFrame" XML attribute. <see
        /// cref="FrameOffset"/> is ignored if this property is <c>true</c>.</remarks>

        public bool UseRandomFrame {
            [DebuggerStepThrough]
            get { return this._useRandomFrame; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                this._useRandomFrame = value;
            }
        }

        #endregion
        #endregion
        #region Private Methods
        #region AreVariablesModified(VariableValueDictionary)

        /// <overloads>
        /// Determines whether the specified variable dictionary of the <see cref="EntityTemplate"/>
        /// contains modifications to the specified variable dictionary of the underlying <see
        /// cref="EntityClass"/>.</overloads>
        /// <summary>
        /// Determines whether the specified <see cref="VariableValueDictionary"/> of the <see
        /// cref="EntityTemplate"/> contains modifications to the specified <see
        /// cref="VariableValueDictionary"/> of the underlying <see cref="EntityClass"/>.</summary>
        /// <param name="classValues">
        /// One of the dictionaries holding the default variable values of the underlying <see
        /// cref="EntityClass"/>.</param>
        /// <param name="templateValues">
        /// One of the dictionaries holding the corresponding variable values of the <see
        /// cref="EntityTemplate"/>.</param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="templateValues"/> collection contains any
        /// key-and-value pairs that are different from those in the specified <paramref
        /// name="classValues"/> collection; otherwise, <c>false</c>.</returns>

        private static bool AreVariablesModified(VariableValueDictionary classValues,
            VariableValueDictionary templateValues) {

            if (templateValues.Count > classValues.Count)
                return true;

            int classValue;
            foreach (var pair in templateValues)
                if (!classValues.TryGetValue(pair.Key, out classValue) || classValue != pair.Value)
                    return true;

            return false;
        }

        #endregion
        #region AreVariablesModified(VariableModifierDictionary)

        /// <summary>
        /// Determines whether the specified <see cref="VariableModifierDictionary"/> of the <see
        /// cref="EntityTemplate"/> contains modifications to the specified <see
        /// cref="VariableModifierDictionary"/> of the underlying <see cref="EntityClass"/>.
        /// </summary>
        /// <param name="classValues">
        /// One of the dictionaries holding the default variable values of the underlying <see
        /// cref="EntityClass"/>.</param>
        /// <param name="templateValues">
        /// One of the dictionaries holding the corresponding variable values of the <see
        /// cref="EntityTemplate"/>.</param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="templateValues"/> collection contains any
        /// key-and-value pairs that are different from those in the specified <paramref
        /// name="classValues"/> collection; otherwise, <c>false</c>.</returns>

        private static bool AreVariablesModified(VariableModifierDictionary classValues,
            VariableModifierDictionary templateValues) {

            if (templateValues.Count > classValues.Count)
                return true;

            VariableModifier classValue;
            foreach (var pair in templateValues)
                if (!classValues.TryGetValue(pair.Key, out classValue) || classValue != pair.Value)
                    return true;

            return false;
        }

        #endregion
        #endregion
        #region GetHashCode

        /// <summary>
        /// Returns the hash code for this <see cref="EntityTemplate"/> instance.</summary>
        /// <returns>
        /// An <see cref="Int32"/> hash code.</returns>
        /// <remarks>
        /// <b>GetHashCode</b> returns the result of <see cref="String.GetHashCode"/> for the <see
        /// cref="EntityClass"/> property of this <see cref="EntityTemplate"/> instance.</remarks>

        public override int GetHashCode() {
            return EntityClass.GetHashCode();
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="EntityTemplate"/>.
        /// </summary>
        /// <returns>
        /// The value of the <see cref="Name"/> property, if it is not an empty string; otherwise,
        /// the value of the <see cref="EntityClass"/> property.</returns>

        public override string ToString() {
            return (Name.Length == 0 ? EntityClass : Name);
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="EntityTemplate"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="EntityTemplate"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="EntityTemplate(EntityTemplate)"/> copy constructor
        /// with this <see cref="EntityTemplate"/> object.</remarks>

        public object Clone() {
            return new EntityTemplate(this);
        }

        #endregion
        #region IEquatable Members
        #region Equals(Object)

        /// <overloads>
        /// Determines whether two <see cref="EntityTemplate"/> instances have the same value.
        /// </overloads>
        /// <summary>
        /// Determines whether this <see cref="EntityTemplate"/> instance and a specified object,
        /// which must be a <see cref="EntityTemplate"/> object, have the same value.</summary>
        /// <param name="obj">
        /// An <see cref="Object"/> to compare to this <see cref="EntityTemplate"/> instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> is another <see cref="EntityTemplate"/> instance
        /// and its value is the same as this instance; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// If the specified <paramref name="obj"/> is another <see cref="EntityTemplate"/>
        /// instance, this method invokes the strongly typed <see cref="Equals(EntityTemplate)"/>
        /// method to test the two instances for value equality.</remarks>

        public override bool Equals(object obj) {

            EntityTemplate template = obj as EntityTemplate;
            if (Object.ReferenceEquals(template, null))
                return false;

            return Equals(template);
        }

        #endregion
        #region Equals(EntityTemplate)

        /// <summary>
        /// Determines whether this instance and a specified <see cref="EntityTemplate"/> object
        /// have the same value.</summary>
        /// <param name="template">
        /// Another <see cref="EntityTemplate"/> object to compare to this instance.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="template"/> is the same as this instance;
        /// otherwise, <c>false</c>.</returns>
        /// <remarks><para>
        /// This method compares the values of all properties except <see cref="Category"/> for the
        /// two <see cref="EntityTemplate"/> objects to test for value equality.
        /// </para><para>
        /// The values of the <see cref="Category"/> property are not compared because
        /// <b>EntityClass</b> strings are unique across all categories.</para></remarks>

        public bool Equals(EntityTemplate template) {

            if (Object.ReferenceEquals(template, null))
                return false;

            return (EntityClass == template.EntityClass
                && FrameOffset == template.FrameOffset
                && Name == template.Name
                && UseRandomFrame == template.UseRandomFrame
                && Attributes.Equals(template.Attributes)
                && AttributeModifiers.Equals(template.AttributeModifiers)
                && Counters.Equals(template.Counters)
                && Resources.Equals(template.Resources)
                && ResourceModifiers.Equals(template.ResourceModifiers));
        }

        #endregion
        #region Equals(EntityTemplate, EntityTemplate)

        /// <summary>
        /// Determines whether two specified <see cref="EntityTemplate"/> objects have the same
        /// value.</summary>
        /// <param name="x">
        /// The first <see cref="EntityTemplate"/> object to compare.</param>
        /// <param name="y">
        /// The second <see cref="EntityTemplate"/> object to compare.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="x"/> is the same as the value of <paramref
        /// name="y"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method invokes the <see cref="Equals(EntityTemplate)"/> instance method to test the
        /// two <see cref="EntityTemplate"/> objects for value equality.</remarks>

        public static bool Equals(EntityTemplate x, EntityTemplate y) {

            if (Object.ReferenceEquals(x, null))
                return Object.ReferenceEquals(y, null);

            if (Object.ReferenceEquals(y, null))
                return false;

            return x.Equals(y);
        }

        #endregion
        #endregion
        #region ScenarioElement Members
        #region ProcessIdentifier

        /// <summary>
        /// Counts, changes, or deletes all occurrences of the specified identifier in the <see
        /// cref="EntityTemplate"/>.</summary>
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
        /// The number of occurrences of <paramref name="oldId"/> in the <see
        /// cref="EntityTemplate"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oldId"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ProcessIdentifier</b> processes <see cref="EntityTemplate"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="EntityClass"/></term><description>As identifier</description>
        /// </item><item>
        /// <term><see cref="Attributes"/><br/> <see cref="AttributeModifiers"/><br/> <see
        /// cref="Counters"/><br/> <see cref="Resources"/><br/> <see cref="ResourceModifiers"/>
        /// </term><description>By key</description>
        /// </item></list></remarks>

        internal override int ProcessIdentifier(string oldId, string newId) {
            int count = 0;

            // process entity class ID
            if (EntityClass == oldId) {
                ++count;
                if (newId != oldId) this._entityClass = newId;
            }

            // process IDs in variable tables
            count += CollectionsUtility.ProcessKey(this._attributes, oldId, newId);
            count += CollectionsUtility.ProcessKey(this._attributeModifiers, oldId, newId);
            count += CollectionsUtility.ProcessKey(this._counters, oldId, newId);
            count += CollectionsUtility.ProcessKey(this._resources, oldId, newId);
            count += CollectionsUtility.ProcessKey(this._resourceModifiers, oldId, newId);

            return count;
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="EntityTemplate"/>.</summary>
        /// <exception cref="XmlException">
        /// Validation failed, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Validate</b> processes <see cref="EntityTemplate"/> properties as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Processing</description>
        /// </listheader><item>
        /// <term><see cref="EntityClass"/></term>
        /// <description>Check against <see cref="EntitySection"/> collection indicated by <see
        /// cref="Category"/></description>
        /// </item><item>
        /// <term><see cref="Attributes"/><br/> <see cref="AttributeModifiers"/><br/> <see
        /// cref="Counters"/><br/> <see cref="Resources"/><br/> <see cref="ResourceModifiers"/>
        /// </term><description>Check identifiers</description>
        /// </item></list><para>
        /// Checks are only performed if <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para></remarks>

        internal override void Validate() {

            // return immediately if editing
            if (ApplicationInfo.IsEditor) return;

            // get entity table for entity category
            var entities = MasterSection.Instance.Entities.GetEntities(Category);

            // check entity class ID
            if (!entities.ContainsKey(EntityClass))
                ThrowHelper.ThrowXmlExceptionWithFormat(
                    Global.Strings.XmlEntityClassInvalid, XmlName, EntityClass);

            // check variable identifiers
            VariableSection variables = MasterSection.Instance.Variables;
            variables.ValidateCollection(Attributes, VariableCategory.Attribute, "Attribute");
            variables.ValidateCollection(AttributeModifiers, VariableCategory.Attribute, "AttributeModifier");
            variables.ValidateCollection(Counters, VariableCategory.Counter, "Counter");
            variables.ValidateCollection(Resources, VariableCategory.Resource, "Resource");
            variables.ValidateCollection(ResourceModifiers, VariableCategory.Resource, "ResourceModifier");
        }

        #endregion
        #endregion
        #region XmlSerializable Members
        #region XmlName

        /// <summary>
        /// Gets the name of the XML element associated with the <see cref="EntityTemplate"/>
        /// object.</summary>
        /// <value>
        /// The value of the constant field <b>ConstXmlName</b> of one of the classes derived from
        /// the <see cref="Scenario.EntityClass"/> class, depending on the value of the <see
        /// cref="Category"/> property.</value>
        /// <exception cref="PropertyValueException">
        /// <see cref="Category"/> is not a valid <see cref="EntityCategory"/> value.</exception>
        /// <remarks>
        /// The actual XML element in <see cref="FilePaths.ScenarioSchema"/> whose data is managed
        /// by the <see cref="EntityTemplate"/> depends on its <see cref="Category"/> value.
        /// </remarks>

        internal override string XmlName {
            [DebuggerStepThrough]
            get {
                switch (Category) {

                    case EntityCategory.Unit:    return UnitClass.ConstXmlName;
                    case EntityCategory.Terrain: return TerrainClass.ConstXmlName;
                    case EntityCategory.Effect:  return EffectClass.ConstXmlName;
                    case EntityCategory.Upgrade: return UpgradeClass.ConstXmlName;

                    default:
                        ThrowHelper.ThrowPropertyValueException(
                            "Category", Category, Tektosyne.Strings.PropertyInvalidValue);
                        return null;
                }
            }
        }

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="EntityTemplate"/> object using the
        /// specified <see cref="XmlReader"/>.</summary>
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

            XmlUtility.ReadAttributeAsString(reader, "id", ref this._entityClass);
            XmlUtility.ReadAttributeAsString(reader, "name", ref this._name);
            XmlUtility.ReadAttributeAsInt32(reader, "frame", ref this._frameOffset);
            XmlUtility.ReadAttributeAsBoolean(reader, "randomFrame", ref this._useRandomFrame);

            // check for presence of attribute since IsVisible is nullable
            string text = reader["visible"];
            if (text != null) this._isVisible = XmlConvert.ToBoolean(text);
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="EntityTemplate"/> object using the specified
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

                default: return false;
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="EntityTemplate"/> object that is serialized to
        /// XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {

            writer.WriteAttributeString("id", EntityClass);
            if (Name.Length > 0)
                writer.WriteAttributeString("name", Name);

            if (UseRandomFrame)
                writer.WriteAttributeString("randomFrame", XmlConvert.ToString(UseRandomFrame));
            else if (FrameOffset > 0)
                writer.WriteAttributeString("frame", XmlConvert.ToString(FrameOffset));

            if (IsVisible != null)
                writer.WriteAttributeString("visible", XmlConvert.ToString(IsVisible.Value));
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="EntityTemplate"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {

            VariableClass.WriteXmlValues(writer, "attribute", Attributes);
            VariableClass.WriteXmlValues(writer, "attributeModifier", AttributeModifiers);
            VariableClass.WriteXmlValues(writer, "counter", Counters);
            VariableClass.WriteXmlValues(writer, "resource", Resources);
            VariableClass.WriteXmlValues(writer, "resourceModifier", ResourceModifiers);
        }

        #endregion
        #endregion
    }
}
