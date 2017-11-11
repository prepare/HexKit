using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;

using Hexkit.Global;
using Hexkit.Scenario;
using Hexkit.World.Commands;
using Hexkit.World.Instructions;

namespace Hexkit.World {
    #region Type Aliases

    using EntityList = KeyedList<String, Entity>;
    using VariableList = KeyedList<String, Variable>;
    using VariableValueDictionary = SortedListEx<String, Int32>;

    #endregion

    /// <summary>
    /// Represents an object that can be owned by a faction or placed on the map.</summary>
    /// <remarks><para>
    /// <b>Entity</b> represents any kind of unit, terrain, effect, or upgrade that can be owned by
    /// a faction or placed on a map site. <b>Entity</b> objects are "instances" of <see
    /// cref="EntityClass"/> objects defined by the current <see cref="EntitySection"/>.
    /// </para><para>
    /// <b>Entity</b> properties are bound by different invariants, depending on the value of the
    /// <see cref="Entity.Category"/> property:
    /// </para><list type="table"><listheader>
    /// <term><see cref="Entity.Category"/></term>
    /// <description><see cref="Entity.Owner"/></description>
    /// <description><see cref="Entity.Site"/></description>
    /// </listheader><item>
    /// <term><see cref="EntityCategory.Effect"/></term>
    /// <description>The value of <b>Site.Owner</b>,<br/> which may be <c>null</c>.</description>
    /// <description>Required.</description>
    /// </item><item>
    /// <term><see cref="EntityCategory.Terrain"/></term>
    /// <description>The value of <b>Site.Owner</b>,<br/> which may be <c>null</c>.</description>
    /// <description>Required.</description>
    /// </item><item>
    /// <term><see cref="EntityCategory.Unit"/></term>
    /// <description>Required; must be identical for<br/> all units at the same <b>Site</b>.
    /// </description><description>Optional.</description>
    /// </item><item>
    /// <term><see cref="EntityCategory.Upgrade"/></term>
    /// <description>Required.</description>
    /// <description>Always a null reference.</description>
    /// </item></list><para>
    /// An "optional" property is one that may return a null reference. A "required" property is one
    /// that may not return a null reference.</para></remarks>

    public abstract class Entity: ICloneable, IKeyedValue<String>, IValuable {
        #region Entity(Entity)

        /// <overloads>
        /// Initializes a new instance of the <see cref="Entity"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="Entity"/> class that is a deep copy of the
        /// specified instance.</summary>
        /// <param name="entity">
        /// The <see cref="Entity"/> object whose properties should be copied to the new instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entity"/> is a null reference.</exception>
        /// <remarks><para>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="entity"/>, whose property values are processed as follows:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Action</description>
        /// </listheader><item>
        /// <term><see cref="EntityClass"/><br/> <see cref="EntityTemplate"/><br/> <see
        /// cref="DisplayClass"/><br/> <see cref="Id"/><br/> <see cref="InstanceName"/></term>
        /// <description>References copied to the new instance.</description>
        /// </item><item>
        /// <term><see cref="AnimationTicks"/><br/> <see cref="FrameOffset"/><br/> <see
        /// cref="IsVisible"/></term>
        /// <description>Values copied to the new instance.</description>
        /// </item><item>
        /// <term><see cref="Attributes"/><br/> <see cref="AttributeModifiers"/><br/> <see
        /// cref="Counters"/><br/> <see cref="Resources"/><br/> <see cref="ResourceModifiers"/>
        /// </term><description>Deep copies assigned to the new instance.</description>
        /// </item><item>
        /// <term><see cref="Category"/><br/> <see cref="DefaultName"/></term>
        /// <description>Values provided by <b>EntityClass</b>.</description>
        /// </item><item>
        /// <term><see cref="Owner"/></term>
        /// <description>Ignored; set by <see cref="Faction(Faction)"/>.</description>
        /// </item><item>
        /// <term><see cref="Site"/></term>
        /// <description>Ignored; set by <see cref="WorldState(WorldState)"/> or <see
        /// cref="World.Site(World.Site)"/>.</description>
        /// </item><item>
        /// <term><see cref="Tag"/></term>
        /// <description>Ignored; current value is lost.</description>
        /// </item></list></remarks>

        protected Entity(Entity entity) {
            if (entity == null)
                ThrowHelper.ThrowArgumentNullException("entity");

            EntityClass = entity.EntityClass;
            EntityTemplate = entity.EntityTemplate;
            Id = entity.Id;

            AnimationTicks = entity.AnimationTicks;
            this._displayClass = entity._displayClass;
            FrameOffset = entity.FrameOffset;
            InstanceName = entity.InstanceName;
            IsVisible = entity.IsVisible;

            Attributes = (VariableContainer) entity.Attributes.Clone();
            AttributeModifiers = (VariableModifierContainer) entity.AttributeModifiers.Clone();
            Counters = (VariableContainer) entity.Counters.Clone();
            Resources = (VariableContainer) entity.Resources.Clone();
            ResourceModifiers = (VariableModifierContainer) entity.ResourceModifiers.Clone();
        }

        #endregion
        #region Entity(EntityClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="Entity"/> class based on the specified <see
        /// cref="Scenario.EntityClass"/>.</summary>
        /// <param name="entityClass">
        /// The initial value for the <see cref="EntityClass"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entityClass"/> is a null reference.</exception>
        /// <remarks><para>
        /// Clients should use factory methods to instantiate the <see cref="Entity"/> class, either
        /// <see cref="CreateEntity"/> or an equivalent method defined by the rule script.
        /// </para><para>
        /// Note that construction does not fully initialize the new instance of the <b>Entity</b>
        /// class. The <see cref="Id"/> property is assigned the <see
        /// cref="Scenario.EntityClass.Id"/> string of the specified <paramref name="entityClass"/>,
        /// but this identifier does not distinguish multiple entities of the same class.
        /// </para><para>
        /// Unless the new <b>Entity</b> instance is a temporary object, clients must therefore call
        /// <see cref="SetUniqueIdentifier"/> to create a new identifier that is unique within a
        /// given <see cref="WorldState"/>.</para></remarks>

        protected Entity(EntityClass entityClass) {
            if (entityClass == null)
                ThrowHelper.ThrowArgumentNullException("entityClass");

            EntityClass = entityClass;
            Id = entityClass.Id;
            IsVisible = entityClass.IsVisible;

            const VariablePurpose purpose = (VariablePurpose.Entity | VariablePurpose.Scenario);
            const VariablePurpose basic = (purpose | VariablePurpose.Basic);
            const VariablePurpose modifier = (purpose | VariablePurpose.Modifier);

            // initialize attributes with scenario values
            Attributes = new VariableContainer(VariableCategory.Attribute,
                basic, EntityClassCache.GetAttributes(entityClass));

            AttributeModifiers = new VariableModifierContainer(VariableCategory.Attribute,
                modifier, EntityClassCache.GetAttributeModifiers(entityClass));

            // initialize counters with scenario values
            Counters = new VariableContainer(VariableCategory.Counter,
                basic, EntityClassCache.GetCounters(entityClass));

            // initialize resources with scenario values
            Resources = new VariableContainer(VariableCategory.Resource,
                basic, EntityClassCache.GetResources(entityClass));

            ResourceModifiers = new VariableModifierContainer(VariableCategory.Resource,
                modifier, EntityClassCache.GetResourceModifiers(entityClass));
        }

        #endregion
        #region Private Fields

        // property backers
        private EntityClass _displayClass;

        #endregion
        #region Public Properties
        #region AnimationTicks

        /// <summary>
        /// Gets or sets the <see cref="DateTime.Ticks"/> for the last animation phase.</summary>
        /// <value>
        /// The <see cref="DateTime.Ticks"/> when the last animation phase for the <see
        /// cref="Entity"/> was triggered. The default is zero.</value>
        /// <remarks>
        /// <b>AnimationTicks</b> is used by the Hexkit graphics engine to update animation frames
        /// if the current <see cref="DisplayClass"/> is animated. The property should be ignored by
        /// other clients.</remarks>

        public long AnimationTicks { get; set; }

        #endregion
        #region Attributes

        /// <summary>
        /// Gets a <see cref="VariableContainer"/> containing all attributes of the <see
        /// cref="Entity"/>.</summary>
        /// <value>
        /// A <see cref="VariableContainer"/> containing the attributes available to the <see
        /// cref="Entity"/>.</value>
        /// <remarks><para>
        /// <b>Attributes</b> never returns a null reference. This property is initialized with the
        /// values of the <see cref="Scenario.EntityClass.Attributes"/> collection of the underlying
        /// <see cref="EntityClass"/>.
        /// </para><para>
        /// Use the HCL instruction <see cref="Command.SetEntityVariable"/> to add or change <see
        /// cref="Variable"/> values while executing a game command.</para></remarks>

        public VariableContainer Attributes { get; private set; }

        #endregion
        #region AttributeModifiers

        /// <summary>
        /// Gets a <see cref="VariableModifierContainer"/> containing all attribute modifiers of the
        /// <see cref="Entity"/>.</summary>
        /// <value>
        /// A <see cref="VariableModifierContainer"/> containing the attribute modifiers defined for
        /// the <see cref="Entity"/>.</value>
        /// <remarks><para>
        /// <b>AttributeModifiers</b> never returns a null reference. This property is initialized
        /// with the values of the <see cref="Scenario.EntityClass.AttributeModifiers"/> collection
        /// of the underlying <see cref="EntityClass"/>.
        /// </para><para>
        /// Use the HCL instruction <see cref="Command.SetEntityVariableModifier"/> to add or change
        /// <see cref="Variable"/> values while executing a game command.</para></remarks>

        public VariableModifierContainer AttributeModifiers { get; private set; }

        #endregion
        #region BlocksAttack

        /// <summary>
        /// Gets a value indicating whether the <see cref="Entity"/> obstructs the line of sight for
        /// ranged attacks.</summary>
        /// <value>
        /// The value of the <see cref="Scenario.EntityClass.BlocksAttack"/> property of the
        /// underlying <see cref="EntityClass"/>.</value>
        /// <remarks>
        /// <see cref="BlocksAttack"/> applies only to ranged attacks by units whose <see
        /// cref="Unit.RangedAttack"/> mode equals <see cref="TargetMode.Line"/>.</remarks>

        public bool BlocksAttack {
            [DebuggerStepThrough]
            get { return EntityClass.BlocksAttack; }
        }

        #endregion
        #region CanDestroy

        /// <summary>
        /// Gets a value indicating whether the <see cref="Entity"/> may be destroyed by its <see
        /// cref="Owner"/>.</summary>
        /// <value>
        /// The value of the <see cref="Hexkit.Scenario.EntityClass.CanDestroy"/> flag of the
        /// underlying <see cref="EntityClass"/>.</value>
        /// <remarks>
        /// If <b>CanDestroy</b> is <c>true</c>, the current <see cref="Owner"/> may issue a <see
        /// cref="DestroyCommand"/> to delete the <see cref="Entity"/>. Otherwise, the <b>Entity</b>
        /// can only be deleted programmatically.</remarks>

        public bool CanDestroy {
            [DebuggerStepThrough]
            get { return EntityClass.CanDestroy; }
        }

        #endregion
        #region Category

        /// <summary>
        /// Gets the entity category of the <see cref="Entity"/>.</summary>
        /// <value>
        /// The value of the <see cref="Scenario.EntityClass.Category"/> property of the underlying
        /// <see cref="EntityClass"/>.</value>
        /// <remarks>
        /// The value of the <b>Category</b> property determines the exact type of this <see
        /// cref="Entity"/> object: <see cref="Unit"/>, <see cref="Terrain"/>, <see cref="Effect"/>,
        /// <see cref="Upgrade"/>, or a derived type that is defined by the rule script.</remarks>

        public EntityCategory Category {
            [DebuggerStepThrough]
            get { return EntityClass.Category; }
        }

        #endregion
        #region Counters

        /// <summary>
        /// Gets a <see cref="VariableContainer"/> containing all counters of the <see
        /// cref="Entity"/>.</summary>
        /// <value>
        /// A <see cref="VariableContainer"/> containing the counters defined for the <see
        /// cref="Entity"/>.</value>
        /// <remarks><para>
        /// <b>Counters</b> never returns a null reference. This property is initialized with the
        /// values of the <see cref="Scenario.EntityClass.Counters"/> collection of the underlying
        /// <see cref="EntityClass"/>.
        /// </para><para>
        /// Use the HCL instruction <see cref="Command.SetEntityVariable"/> to add or change <see
        /// cref="Variable"/> values while executing a game command.</para></remarks>

        public VariableContainer Counters { get; private set; }

        #endregion
        #region DefaultName

        /// <summary>
        /// Gets the default name of the <see cref="Entity"/>.</summary>
        /// <value>
        /// The value of the <see cref="Scenario.EntityClass.Name"/> property of the underlying <see
        /// cref="EntityClass"/>.</value>

        public string DefaultName {
            [DebuggerStepThrough]
            get { return EntityClass.Name; }
        }

        #endregion
        #region DisplayClass

        /// <summary>
        /// Gets or sets the <see cref="Scenario.EntityClass"/> providing the image frames that
        /// represent the <see cref="Entity"/>.</summary>
        /// <value>
        /// The <see cref="Scenario.EntityClass"/> whose <see
        /// cref="Scenario.EntityClass.ImageStack"/> represents the <see cref="Entity"/> on the map
        /// view. The default is the value of the <see cref="EntityClass"/> property.</value>
        /// <remarks><para>
        /// Use the HCL instruction <see cref="Command.SetEntityDisplayClass"/> to set this property
        /// while executing a game command. Hexkit itself never changes this property.
        /// </para><para>
        /// Setting <b>DisplayClass</b> to a null reference resets the property to its default
        /// value, i.e. the value of the <see cref="EntityClass"/> property.
        /// </para><para>
        /// Changing the value of <b>DisplayClass</b> also resets <see cref="FrameOffset"/> to zero.
        /// This ensures that <b>FrameOffset</b> is valid for the new <b>DisplayClass</b>.
        /// </para></remarks>

        public EntityClass DisplayClass {
            [DebuggerStepThrough]
            get { return this._displayClass ?? EntityClass; }
            internal set {
                // ensure default value of null
                if (value == EntityClass)
                    value = null;

                // reset frame when changing classes
                if (value != this._displayClass) {
                    this._displayClass = value;
                    FrameOffset = 0;
                }
            }
        }

        #endregion
        #region EntityClass

        /// <summary>
        /// Gets the scenario class of the <see cref="Entity"/>.</summary>
        /// <value>
        /// The <see cref="Scenario.EntityClass"/> on which the <see cref="Entity"/> is based.
        /// </value>
        /// <remarks><para>
        /// <b>EntityClass</b> never returns a null reference. This property is set by the method 
        /// <see cref="SetEntityClass"/>. Use the HCL instruction <see
        /// cref="Command.SetEntityClass"/> to set this property while executing a game command.
        /// </para><para>
        /// Derived classes provide variants of this property that return the same object but cast
        /// to the appropriate derived type: <see cref="UnitClass"/>, <see cref="TerrainClass"/>,
        /// <see cref="EffectClass"/>, or <see cref="UpgradeClass"/>.</para></remarks>

        public EntityClass EntityClass { get; private set; }

        #endregion
        #region EntityTemplate

        /// <summary>
        /// Gets the scenario template for the <see cref="Entity"/>, if any.</summary>
        /// <value><para>
        /// The original <see cref="Scenario.EntityTemplate"/> for the <see cref="Entity"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if the <see cref="Entity"/> was not created from an <see
        /// cref="Scenario.EntityTemplate"/>.</para></value>
        /// <remarks><para>
        /// <b>EntityTemplate</b> is set by <see cref="SetEntityTemplate"/> which is called only
        /// once, immediately after construction, if the <see cref="Entity"/> was created from a
        /// scenario template.
        /// </para><para>
        /// If <see cref="ApplicationInfo.IsEditor"/> is <c>true</c>, <b>EntityTemplate</b> holds
        /// a deep copy of the original <see cref="Scenario.EntityTemplate"/> object, rather than
        /// the object itself. This allows Hexkit Editor to change an <b>EntityTemplate</b> without
        /// affecting other entities created from the same <see cref="Scenario.Area"/> which might
        /// cover multiple sites.</para></remarks>

        public EntityTemplate EntityTemplate { get; private set; }

        #endregion
        #region FrameOffset

        /// <summary>
        /// Gets or sets the index offset of the image frame that represents the <see
        /// cref="Entity"/>.</summary>
        /// <value>
        /// The offset that is added to the <see cref="Scenario.EntityClass.FrameIndex"/> of the
        /// current <see cref="DisplayClass"/> to obtain the bitmap catalog index of the image frame
        /// that represents the <see cref="Entity"/>. The default is zero.</value>
        /// <remarks><para>
        /// If the current <see cref="DisplayClass"/> is not animated, <b>FrameOffset</b> allows
        /// clients to select a specific image frame for display. Use the HCL instruction <see
        /// cref="Command.SetEntityFrameOffset"/> to set this property while executing a game
        /// command.
        /// </para><para>
        /// If the current <see cref="DisplayClass"/> is animated, <b>FrameOffset</b> is updated
        /// automatically by the Hexit graphics engine to reflect the current animation frame. In
        /// this case, the property may contain negative values to indicate backward progress.
        /// </para></remarks>

        public int FrameOffset { get; set; }

        #endregion
        #region HasUniqueName

        /// <summary>
        /// Gets a value indicating whether <see cref="Name"/> equals <see cref="DefaultName"/> with
        /// a unique suffix.</summary>
        /// <value>
        /// <c>true</c> if <see cref="Name"/> equals <see cref="DefaultName"/> with a suffix that
        /// could have been created by <see cref="SetUniqueName"/>; otherwise, <c>false</c>.</value>
        /// <remarks><para>
        /// <b>HasUniqueName</b> uses the following heuristics to determine whether the current <see
        /// cref="Name"/> could have been set by <see cref="SetUniqueName"/>:
        /// </para><list type="number"><item>
        /// <see cref="InstanceName"/> must be a non-empty string.
        /// </item><item>
        /// <see cref="InstanceName"/> must start with <see cref="DefaultName"/> and exceed its
        /// length by at least three characters.
        /// </item><item>
        /// The remaining characters which compose the unique suffix must be a space, a hash sign
        /// (#), and one or more digits.</item></list></remarks>

        public bool HasUniqueName {
            get {
                // unique name requires instance name
                if (String.IsNullOrEmpty(InstanceName))
                    return false;

                // check for default name with room for suffix
                int i = DefaultName.Length;
                if (InstanceName.Length < i + 3 ||
                    !InstanceName.StartsWith(DefaultName, StringComparison.Ordinal))
                    return false;

                // check for " #" sequence before digits
                bool isUnique = (InstanceName[i++] == ' ');
                isUnique &= (InstanceName[i++] == '#');

                // check for digits up to end of instance name
                for (; i < InstanceName.Length; i++)
                    isUnique &= Char.IsDigit(InstanceName, i);

                return isUnique;
            }
        }

        #endregion
        #region Id

        /// <summary>
        /// Gets the identifier of the <see cref="Entity"/>.</summary>
        /// <value>
        /// The unique identifier of the <see cref="Entity"/>. This value equals the <see
        /// cref="Scenario.EntityClass.Id"/> property of the underlying <see cref="EntityClass"/>,
        /// followed by a dash ("-") and the <see cref="WorldState.InstanceCounts"/> for the
        /// <b>EntityClass</b> in the current <see cref="WorldState"/>.</value>
        /// <remarks><para>
        /// <b>Id</b> never returns a null reference or an empty string. This property never changes
        /// once <see cref="SetUniqueIdentifier"/> has been executed, which creates the value
        /// described above.
        /// </para><para>
        /// <b>Id</b> returns an internal identifier that is not intended for display in Hexkit
        /// Game.</para></remarks>

        public string Id { get; private set; }

        #endregion
        #region InstanceName

        /// <summary>
        /// Gets the instance name of the <see cref="Entity"/>.</summary>
        /// <value>
        /// The value of the <see cref="Name"/> property, if different from <see
        /// cref="DefaultName"/>; otherwise, a null reference.</value>
        /// <remarks>
        /// <b>InstanceName</b> is set whenever the <see cref="Name"/> property changes.</remarks>

        public string InstanceName { get; private set; }

        #endregion
        #region IsModifiable

        /// <summary>
        /// Gets a value indicating whether <see cref="Variable"/> modifiers are automatically
        /// applied to the <see cref="Entity"/>.</summary>
        /// <value>
        /// <c>true</c> if modifiers are automatically applied to the <see cref="Attributes"/> and
        /// <see cref="Resources"/> collections; otherwise, <c>false</c>.</value>
        /// <exception cref="PropertyValueException">
        /// <see cref="Category"/> is not a valid <see cref="EntityCategory"/> value.</exception>
        /// <remarks><para>
        /// <b>IsModifiable</b> depends on the value of <see cref="Category"/>, as follows:
        /// </para><list type="table"><listheader>
        /// <term><see cref="Category"/></term><description><b>IsModifiable</b></description>
        /// </listheader><item>
        /// <term><see cref="EntityCategory.Unit"/></term>
        /// <description><see cref="IsOwned"/> &amp;&amp; <see cref="IsPlaced"/></description>
        /// </item><item>
        /// <term><see cref="EntityCategory.Terrain"/></term>
        /// <description><see cref="IsPlaced"/></description>
        /// </item><item>
        /// <term><see cref="EntityCategory.Effect"/></term>
        /// <description><see cref="IsPlaced"/></description>
        /// </item><item>
        /// <term><see cref="EntityCategory.Upgrade"/></term>
        /// <description><see cref="IsOwned"/></description></item></list></remarks>

        public bool IsModifiable {
            get {
                switch (Category) {

                    case EntityCategory.Unit:
                        return (IsOwned && IsPlaced);

                    case EntityCategory.Terrain:
                    case EntityCategory.Effect:
                        return IsPlaced;

                    case EntityCategory.Upgrade:
                        return IsOwned;

                    default:
                        ThrowHelper.ThrowPropertyValueException("Category",
                            Tektosyne.Strings.PropertyInvalidValue);
                        return false;
                }
            }
        }

        #endregion
        #region IsOwned

        /// <summary>
        /// Gets a value indicating whether the <see cref="Entity"/> is owned by a <see
        /// cref="Faction"/>.</summary>
        /// <value>
        /// <c>true</c> if the current <see cref="Owner"/> is not a null reference; otherwise,
        /// <c>false</c>.</value>

        public bool IsOwned {
            [DebuggerStepThrough]
            get { return (Owner != null); }
        }

        #endregion
        #region IsPlaced

        /// <summary>
        /// Gets a value indicating whether the <see cref="Entity"/> is placed on the map.</summary>
        /// <value>
        /// <c>true</c> if the current <see cref="Site"/> is not a null reference; otherwise,
        /// <c>false</c>.</value>

        public bool IsPlaced {
            [DebuggerStepThrough]
            get { return (Site != null); }
        }

        #endregion
        #region IsVisible

        /// <summary>
        /// Gets a value indicating whether the <see cref="Entity"/> is visible on map views.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="Entity"/> is visible on map views; otherwise,
        /// <c>false</c>.</value>
        /// <remarks><para>
        /// <b>IsVisible</b> determines whether the <see cref="Entity"/> is drawn on <em>any</em>
        /// map views, regardless of whether the <see cref="WorldState.ActiveFaction"/> can observe
        /// the <see cref="Entity"/>. This property has no effect on the visual appearance of the
        /// <see cref="Entity"/> on other displays, and no gameplay effects whatsoever.
        /// </para><para>
        /// <b>IsVisible</b> usually returns the same value as the <see
        /// cref="Scenario.EntityClass.IsVisible"/> flag of the underlying <see
        /// cref="EntityClass"/>. However, if the <see cref="Entity"/> was created from an <see
        /// cref="EntityTemplate"/> whose <see cref="Scenario.EntityTemplate.IsVisible"/> flag was
        /// not a null reference, <b>IsVisible</b> returns that value instead.</para></remarks>

        public bool IsVisible { get; private set; }

        #endregion
        #region Name

        /// <summary>
        /// Gets or sets the display name of the <see cref="Entity"/>.</summary>
        /// <value>
        /// The display name of the <see cref="Entity"/>. The default is the value of the <see
        /// cref="DefaultName"/> property.</value>
        /// <remarks><para>
        /// <b>Name</b> returns the name that should be used to represent the <see cref="Entity"/>
        /// within Hexkit Game.
        /// </para><para>
        /// Use the HCL instruction <see cref="Command.SetEntityName"/> to set this property while
        /// executing a game command.
        /// </para><para>
        /// Setting <b>Name</b> to a null reference or an empty string resets the property to its
        /// default value, i.e. the value of the <see cref="DefaultName"/> property. In this case,
        /// or if the new value equals <b>DefaultName</b>, the <see cref="InstanceName"/> property
        /// is set to a null reference; otherwise, <b>InstanceName</b> is set to the new value of
        /// <b>Name</b>.</para></remarks>

        public string Name {
            [DebuggerStepThrough]
            get { return StringUtility.Validate(InstanceName, DefaultName); }
            internal set {
                if (value == DefaultName || String.IsNullOrEmpty(value))
                    InstanceName = null;
                else
                    InstanceName = String.Intern(value);
            }
        }

        #endregion
        #region Owner

        /// <summary>
        /// Gets the <see cref="Faction"/> that owns the <see cref="Entity"/>.</summary>
        /// <value><para>
        /// The <see cref="Faction"/> that owns the <see cref="Entity"/>.
        /// </para><para>-or-</para><para>
        /// A null reference to indicate an unowned <b>Entity</b>. The default is a null
        /// reference.</para></value>
        /// <remarks>
        /// <b>Owner</b> is set by the method <see cref="SetOwner"/>. Use the HCL instruction <see
        /// cref="Command.SetEntityOwner"/> to set this property while executing a game command.
        /// </remarks>

        public Faction Owner { get; private set; }

        #endregion
        #region Resources

        /// <summary>
        /// Gets a <see cref="VariableContainer"/> containing all resources of the <see
        /// cref="Entity"/>.</summary>
        /// <value>
        /// A <see cref="VariableContainer"/> containing the resources available to the <see
        /// cref="Entity"/>.</value>
        /// <remarks><para>
        /// <b>Resources</b> never returns a null reference. This property is initialized with the
        /// values of the <see cref="Scenario.EntityClass.Resources"/> collection of the underlying
        /// <see cref="EntityClass"/>.
        /// </para><para>
        /// Use the HCL instruction <see cref="Command.SetEntityVariable"/> to add or change <see
        /// cref="Variable"/> values while executing a game command.</para></remarks>

        public VariableContainer Resources { get; private set; }

        #endregion
        #region ResourceModifiers

        /// <summary>
        /// Gets a <see cref="VariableModifierContainer"/> containing all resource modifiers of the
        /// <see cref="Entity"/>.</summary>
        /// <value>
        /// A <see cref="VariableModifierContainer"/> containing the resource modifiers defined for
        /// the <see cref="Entity"/>.</value>
        /// <remarks><para>
        /// <b>ResourceModifiers</b> never returns a null reference. This property is initialized
        /// with the values of the <see cref="Scenario.EntityClass.ResourceModifiers"/> collection
        /// of the underlying <see cref="EntityClass"/>.
        /// </para><para>
        /// Use the HCL instruction <see cref="Command.SetEntityVariableModifier"/> to add or change
        /// <see cref="Variable"/> values while executing a game command.</para></remarks>

        public VariableModifierContainer ResourceModifiers { get; private set; }

        #endregion
        #region Site

        /// <summary>
        /// Gets the <see cref="World.Site"/> occupied by the <see cref="Entity"/>.</summary>
        /// <value><para>
        /// The <see cref="World.Site"/> occupied by the <see cref="Entity"/>.
        /// </para><para>-or-</para><para>
        /// A null reference to indicate an unplaced <b>Entity</b>. The default is a null
        /// reference.</para></value>
        /// <remarks>
        /// <b>Site</b> is set by the method <see cref="SetSite"/>. Use the HCL instruction <see
        /// cref="Command.SetEntitySite"/> to set this property while executing a game command.
        /// </remarks>

        public Site Site { get; private set; }

        #endregion
        #region Tag

        /// <summary>
        /// Gets or sets an <see cref="Object"/> that contains temporary data about the <see
        /// cref="Entity"/>.</summary>
        /// <value>
        /// An <see cref="Object"/> that contains temporary data about the <see cref="Entity"/>. The
        /// default is a null reference.</value>
        /// <remarks><para>
        /// <b>Tag</b> provides temporary data storage for computer player algorithms. The value of
        /// this property is not persisted to Hexkit session files, nor copied by <see
        /// cref="Clone"/> or the copy constructor.
        /// </para><note type="caution">
        /// <b>Tag</b> is reserved for computer player algorithms and should not be used elsewhere.
        /// Rule scripts in particular should define their own additional data fields in
        /// <b>CustomUnit</b> or derived classes as needed.</note></remarks>

        public object Tag { get; set; }

        #endregion
        #region UniqueNameSuffix

        /// <summary>
        /// Gets the unique suffix, if any, that follows <see cref="DefaultName"/> in <see
        /// cref="Name"/>.</summary>
        /// <value>
        /// The suffix following <see cref="DefaultName"/> in <see cref="Name"/> that could have
        /// been created by <see cref="SetUniqueName"/>, if any; otherwise, a null reference.
        /// </value>
        /// <remarks>
        /// <b>UniqueNameSuffix</b> evaluates <see cref="HasUniqueName"/> to determine whether the
        /// current <see cref="Name"/> contains a unique suffix.</remarks>

        public string UniqueNameSuffix {
            get {
                if (HasUniqueName) {
                    Debug.Assert(InstanceName.Length >= DefaultName.Length + 3);
                    return InstanceName.Substring(DefaultName.Length);
                }

                return null;
            }
        }

        #endregion
        #endregion
        #region Internal Methods
        #region CopyCollection(EntityList, Faction)

        /// <overloads>
        /// Creates a deep copy of the specified <see cref="Entity"/> collection and sets the
        /// specified property of all elements.</overloads>
        /// <summary>
        /// Creates a deep copy of the specified <see cref="Entity"/> collection and sets the <see
        /// cref="Owner"/> property of all elements.</summary>
        /// <param name="entities">
        /// The <see cref="EntityList"/> to copy.</param>
        /// <param name="owner">
        /// The initial value for the <see cref="Owner"/> property of each element in the copied
        /// collection.</param>
        /// <returns>
        /// A deep copy of the specified <paramref name="entities"/> collection, with the <see
        /// cref="Owner"/> property of each element set to the specified <paramref name="owner"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entities"/> or <paramref name="owner"/> is a null reference.</exception>
        /// <remarks>
        /// <b>CopyCollection</b> calls <see cref="EntityList.Copy"/> on the specified <paramref
        /// name="entities"/> collection, which in turn invokes <see cref="Clone"/> on each element.
        /// </remarks>

        internal static EntityList CopyCollection(EntityList entities, Faction owner) {
            if (entities == null)
                ThrowHelper.ThrowArgumentNullException("entities");
            if (owner == null)
                ThrowHelper.ThrowArgumentNullException("owner");

            // Copy calls Clone on each element
            EntityList copy = entities.Copy();

            // set backer directly, bypass setter
            for (int i = 0; i < copy.Count; i++)
                copy[i].Owner = owner;

            return copy;
        }

        #endregion
        #region CopyCollection(EntityList, Site)

        /// <summary>
        /// Creates a deep copy of the specified <see cref="Entity"/> collection and sets the <see
        /// cref="Site"/> property of all elements.</summary>
        /// <param name="entities">
        /// The <see cref="EntityList"/> to copy.</param>
        /// <param name="site">
        /// The initial value for the <see cref="Site"/> property of each element in the copied
        /// collection.</param>
        /// <returns>
        /// A deep copy of the specified <paramref name="entities"/> collection, with the <see
        /// cref="Site"/> property of each element set to the specified <paramref name="site"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entities"/> or <paramref name="site"/> is a null reference.</exception>
        /// <remarks>
        /// <b>CopyCollection</b> calls <see cref="EntityList.Copy"/> on the specified <paramref
        /// name="entities"/> collection, which in turn invokes <see cref="Clone"/> on each element.
        /// </remarks>

        internal static EntityList CopyCollection(EntityList entities, Site site) {
            if (entities == null)
                ThrowHelper.ThrowArgumentNullException("entities");
            if (site == null)
                ThrowHelper.ThrowArgumentNullException("site");

            // Copy calls Clone on each element
            EntityList copy = entities.Copy();

            // set backer directly, bypass setter
            for (int i = 0; i < copy.Count; i++)
                copy[i].Site = site;

            return copy;
        }

        #endregion
        #region CreateEntity

        /// <summary>
        /// Creates a new <see cref="Entity"/> object from the specified <see
        /// cref="Scenario.EntityClass"/>.</summary>
        /// <param name="entityClass">
        /// The <see cref="Scenario.EntityClass"/> to instantiate.</param>
        /// <returns>
        /// A new <see cref="Entity"/> object based on the specified <paramref name="entityClass"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entityClass"/> is a null reference.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="entityClass"/> specifies an invalid <see
        /// cref="Scenario.EntityClass.Category"/> value.</exception>
        /// <remarks><para>
        /// <b>CreateEntity</b> is the default factory method for <see cref="Entity"/> objects. All
        /// it does is return the result of the constructor for the <b>Entity</b> type that
        /// corresponds to the <see cref="Scenario.EntityClass.Category"/> of the specified
        /// <paramref name="entityClass"/>.
        /// </para><para>
        /// <b>CreateEntity</b> conforms to the <see cref="CreateEntityCallback"/> delegate.
        /// </para></remarks>

        internal static Entity CreateEntity(EntityClass entityClass) {
            if (entityClass == null)
                ThrowHelper.ThrowArgumentNullException("entityClass");

            switch (entityClass.Category) {

                case EntityCategory.Unit:
                    return new Unit((UnitClass) entityClass);

                case EntityCategory.Terrain:
                    return new Terrain((TerrainClass) entityClass);

                case EntityCategory.Effect:
                    return new Effect((EffectClass) entityClass);

                case EntityCategory.Upgrade:
                    return new Upgrade((UpgradeClass) entityClass);

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException("entityClass.Category",
                        (int) entityClass.Category, typeof(EntityCategory));
                    return null;
            }
        }

        #endregion
        #region SetEntityClass

        /// <summary>
        /// Sets the <see cref="EntityClass"/> property to the specified value.</summary>
        /// <param name="entityClass">
        /// The new value for the <see cref="EntityClass"/> property.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="entityClass"/> specifies a <see cref="Scenario.EntityClass.Category"/>
        /// that is different from the current <see cref="Category"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entityClass"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetEntityClass</b> performs the following actions on the properties of this <see
        /// cref="Entity"/> object:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Action</description>
        /// </listheader><item>
        /// <term><see cref="EntityClass"/></term>
        /// <description>Set to the specified <paramref name="entityClass"/>.</description>
        /// </item><item>
        /// <term><see cref="InstanceName"/></term>
        /// <description>Set to a new unique name for the new <b>EntityClass</b> and the current
        /// <see cref="Owner"/> if <see cref="HasUniqueName"/> returns <c>true</c>; otherwise,
        /// remains unchanged.</description>
        /// </item><item>
        /// <term><see cref="Attributes"/><br/> <see cref="AttributeModifiers"/><br/> <see
        /// cref="ResourceModifiers"/></term>
        /// <description>Set to the default collections for the new <b>EntityClass</b>. No existing
        /// values are transferred.</description>
        /// </item><item>
        /// <term><see cref="Counters"/></term>
        /// <description>Set to the default collection for the new <b>EntityClass</b>. Any existing
        /// values are then transferred, even if they do not exist in the default collection.
        /// </description></item><item>
        /// <term><see cref="Resources"/></term>
        /// <description>Set to the default collection for the new <b>EntityClass</b>. Any existing
        /// values are then transferred, but only if they also exist in the default collection.
        /// </description></item></list><para>
        /// All other properties of <see cref="Entity"/> or derived classes remain unchanged, or
        /// reflect the new <b>EntityClass</b>, as the case may be.
        /// </para><note type="caution">
        /// The <see cref="Id"/> and <see cref="EntityTemplate"/> properties are <em>not</em>
        /// changed and will therefore no longer reflect the new <b>EntityClass</b>.
        /// </note></remarks>

        internal void SetEntityClass(EntityClass entityClass) {
            if (entityClass == null)
                ThrowHelper.ThrowArgumentNullException("entityClass");

            if (entityClass.Category != Category)
                ThrowHelper.ThrowArgumentExceptionWithFormat(
                    "entityClass", Tektosyne.Strings.ArgumentPropertyConflict, "Category");

            // recreate unique name if present
            bool hasUniqueName = HasUniqueName;
            EntityClass = entityClass;
            if (hasUniqueName) SetUniqueName(Owner);

            // reset attributes and modifiers to scenario values
            Attributes.SetVariables(EntityClassCache.GetAttributes(entityClass));
            AttributeModifiers.SetVariables(EntityClassCache.GetAttributeModifiers(entityClass));
            ResourceModifiers.SetVariables(EntityClassCache.GetResourceModifiers(entityClass));

            // reset counters to scenario values and transfer values
            var originalCounters = Counters.Variables;
            Counters.SetVariables(EntityClassCache.GetCounters(entityClass));
            foreach (Variable variable in originalCounters)
                Counters.SetValue(variable.VariableClass, variable.Value, false);

            // reset resources to scenario values and transfer changes
            var originalResources = Resources.ToDictionary();
            Resources.SetVariables(EntityClassCache.GetResources(entityClass));
            Resources.ImportChanges(originalResources);
        }

        #endregion
        #region SetEntityTemplate

        /// <summary>
        /// Sets the <see cref="EntityTemplate"/> property to the specified value and updates all
        /// related property values accordingly.</summary>
        /// <param name="template">
        /// The new value for the <see cref="EntityTemplate"/> property.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="template"/> specifies an <see
        /// cref="Scenario.EntityTemplate.EntityClass"/> that differs from the underlying <see
        /// cref="EntityClass"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="template"/> is a null reference.</exception>
        /// <remarks>
        /// <b>SetEntityTemplate</b> also updates the values of the <see cref="Name"/>, <see
        /// cref="FrameOffset"/>, and <see cref="IsVisible"/> properties with those of the specified
        /// <paramref name="template"/>, and imports any changes made by the <paramref
        /// name="template"/> to the <see cref="Attributes"/>, <see cref="AttributeModifiers"/>,
        /// <see cref="Counters"/>, <see cref="Resources"/>, and <see cref="ResourceModifiers"/>
        /// collections.</remarks>

        internal void SetEntityTemplate(EntityTemplate template) {
            if (template == null)
                ThrowHelper.ThrowArgumentNullException("template");

            if (template.EntityClass != EntityClass.Id)
                ThrowHelper.ThrowArgumentExceptionWithFormat(
                    "template", Tektosyne.Strings.ArgumentPropertyConflict, "EntityClass");

            /*
             * Hexkit Editor may edit templates, and a single template is shared by many
             * entities if it was defined by an Area object that covers multiple sites.
             * 
             * Therefore, we must store a clone rather than the original template when
             * in editing mode, so that editing does not affect many different entities.
             */

            EntityTemplate = (ApplicationInfo.IsEditor ? 
                new EntityTemplate(template) : template);

            Name = template.Name;
            if (template.UseRandomFrame) {
                int maxOffset = DisplayClass.FrameCount - 1;
                FrameOffset = (maxOffset < 1 ? 0 : MersenneTwister.Default.Next(maxOffset));
            } else
                FrameOffset = template.FrameOffset;

            // adopt map view visibility of template
            if (template.IsVisible != null)
                IsVisible = template.IsVisible.Value;

            Attributes.ImportChanges(template.Attributes);
            AttributeModifiers.ImportChanges(template.AttributeModifiers);
            Counters.ImportChanges(template.Counters);
            Resources.ImportChanges(template.Resources);
            ResourceModifiers.ImportChanges(template.ResourceModifiers);
        }

        #endregion
        #region SetOwner

        /// <summary>
        /// Sets the <see cref="Owner"/> property to the specified value.</summary>
        /// <param name="owner">
        /// The new value for the <see cref="Owner"/> property.</param>
        /// <param name="validate">
        /// <c>true</c> to validate the specified <paramref name="owner"/> against the invariants of
        /// this <see cref="Entity"/>; otherwise, <c>false</c>.</param>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="validate"/> is <c>true</c>, <see cref="ApplicationInfo.IsEditor"/> is
        /// <c>false</c>, and <paramref name="owner"/> violates an invariant of this <see
        /// cref="Entity"/>.</exception>
        /// <remarks><para>
        /// If <paramref name="validate"/> is <c>true</c> and <see cref="ApplicationInfo.IsEditor"/>
        /// is <c>false</c>, <b>SetOwner</b> first invokes <see cref="ValidateOwner"/> to validate
        /// the specified <paramref name="owner"/> against the invariants of this <see
        /// cref="Entity"/>.
        /// </para><para>
        /// Once validation has been skipped or succeeded, <b>SetOwner</b> performs the following
        /// actions:
        /// </para><list type="number"><item>
        /// Remove the <see cref="Entity"/> from the matching collection of its previous <see
        /// cref="Owner"/>, if any.
        /// </item><item>
        /// Change the <see cref="Owner"/> property to the specified <paramref name="owner"/>.
        /// </item><item>
        /// Add the <see cref="Entity"/> to the matching collection of its new <see cref="Owner"/>,
        /// if any.</item></list></remarks>

        internal void SetOwner(Faction owner, bool validate) {

            // check against owner invariants
            if (validate && !ApplicationInfo.IsEditor)
                ValidateOwner(owner);

            // remove from old faction, if any
            if (Owner != null) {
                EntityList entities = Owner.GetWritableEntities(Category);

                // remove by index to provoke exception if missing
                if (entities != null) {
                    int index = entities.IndexOf(this);
                    entities.RemoveAt(index);
                }
            }

            Owner = owner;

            // add to new faction, if any
            if (owner != null) {
                EntityList entities = owner.GetWritableEntities(Category);
                if (entities != null) entities.Add(this);
            }
        }

        #endregion
        #region SetOwnerUnchecked

        /// <summary>
        /// Sets the <see cref="Owner"/> property to the specified value, without removing the <see
        /// cref="Entity"/> from its previous owner.</summary>
        /// <param name="owner">
        /// The new value for the <see cref="Owner"/> property. This argument cannot be a null
        /// reference.</param>
        /// <remarks><para>
        /// <b>SetOwnerUnchecked</b> is a fast, but unsafe, helper method for the <see
        /// cref="WorldState(WorldState)"/> copy constructor and should not normally be called by
        /// other clients.
        /// </para><para>
        /// <b>SetOwnerUnchecked</b> sets the <see cref="Owner"/> property to the specified
        /// <paramref name="owner"/> and then adds the <see cref="Entity"/> to that <see
        /// cref="Faction"/>. All other checks and actions performed by the <see cref="SetOwner"/>
        /// method are skipped.</para></remarks>

        internal void SetOwnerUnchecked(Faction owner) {

            Debug.Assert(Owner == null);
            Debug.Assert(owner != null);

            Owner = owner;
            owner.GetWritableEntities(Category).Add(this);
        }

        #endregion
        #region SetSite

        /// <summary>
        /// Sets the <see cref="Site"/> property to the specified value.</summary>
        /// <param name="site">
        /// The new value for the <see cref="Site"/> property.</param>
        /// <param name="validate">
        /// <c>true</c> to validate the specified <paramref name="site"/> against the invariants of
        /// this <see cref="Entity"/>; otherwise, <c>false</c>.</param>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="validate"/> is <c>true</c>, <see cref="ApplicationInfo.IsEditor"/> is
        /// <c>false</c>, and <paramref name="site"/> violates an invariant of this <see
        /// cref="Entity"/>.</exception>
        /// <remarks><para>
        /// If <paramref name="validate"/> is <c>true</c> and <see cref="ApplicationInfo.IsEditor"/>
        /// is <c>false</c>, <b>SetSite</b> first invokes <see cref="ValidateSite"/> to validate the
        /// specified <paramref name="site"/> against the invariants of this <see cref="Entity"/>.
        /// </para><para>
        /// Once validation has been skipped or succeeded, <b>SetSite</b> performs the following
        /// actions:
        /// </para><list type="number"><item>
        /// Remove the <see cref="Entity"/> from the matching stack of its previous <see
        /// cref="Site"/>, if any.
        /// </item><item>
        /// Change the <see cref="Site"/> property to the specified <paramref name="site"/>.
        /// </item><item>
        /// Add the <see cref="Entity "/>to the matching stack of its new <see cref="Site"/>, if
        /// any.</item></list></remarks>

        internal void SetSite(Site site, bool validate) {

            // check against site invariants
            if (validate && !ApplicationInfo.IsEditor)
                ValidateSite(site);

            // remove from old site, if any
            if (Site != null) {
                EntityList entities = Site.GetWritableEntities(Category);

                // remove by index to provoke exception if missing
                int index = entities.IndexOf(this);
                entities.RemoveAt(index);
            }

            Site = site;

            // add to new site, if any
            if (site != null)
                site.GetWritableEntities(Category).Add(this);
        }

        #endregion
        #region SetUniqueIdentifier

        /// <summary>
        /// Sets the <see cref="Id"/> property to a unique identifier.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> within whose <see cref="WorldState.Entities"/> the new <see
        /// cref="Id"/> should be unique.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidOperationException">
        /// <see cref="Id"/> has already been changed from the <see cref="EntityClass"/> identifier.
        /// </exception>
        /// <remarks><para>
        /// <b>SetUniqueIdentifier</b> replaces the temporary <see cref="Id"/> string assigned by
        /// the <see cref="Entity(EntityClass)"/> constructor with a new identifier that is unique
        /// within the specified <paramref name="worldState"/>.
        /// </para><para>
        /// The new <b>Id</b> consists of the old <b>Id</b> followed by a dash ("-") and the current
        /// <see cref="WorldState.InstanceCounts"/> of the underlying <see cref="EntityClass"/>
        /// within the specified <paramref name="worldState"/>. That is, the first <b>Id</b> for a
        /// given <see cref="EntityClass"/> receives the number zero.
        /// </para><para>
        /// After <b>Id</b> has been set, <b>SetUniqueIdentifier</b> also adds the <see
        /// cref="Entity"/> to the <see cref="WorldState.Entities"/> collection of the specified
        /// <paramref name="worldState"/> and increments its <b>InstanceCount</b>.
        /// </para><para>
        /// The new <b>Id</b> is final. Invoking <b>SetUniqueIdentifier</b> again on this <see
        /// cref="Entity"/> will throw an <see cref="InvalidOperationException"/>.</para></remarks>

        internal void SetUniqueIdentifier(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (Id != EntityClass.Id)
                ThrowHelper.ThrowInvalidOperationException(Global.Strings.ErrorIdentifierChanged);

            // retrieve and increment instance count
            int count = worldState.AddInstance(EntityClass);

            // construct unique ID with instance count
            Id = String.Intern(String.Format(CultureInfo.InvariantCulture, "{0}-{1}", Id, count));

            // add to global entity table
            worldState.WritableEntities.Add(Id, this);
        }

        #endregion
        #region SetUniqueName

        /// <summary>
        /// Sets the <see cref="Name"/> property to a name that is unique within the possessions of
        /// the specified <see cref="Faction"/>.</summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> within whose possessions the <see cref="Name"/> should be
        /// unique.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>SetUniqueName</b> sets the <see cref="Name"/> property to a string that is unique
        /// within the possessions of the specified <paramref name="faction"/>.
        /// </para><para>
        /// <b>SetUniqueName</b> first increments the <see cref="Faction.InstanceCounts"/> of the
        /// underlying <see cref="EntityClass"/> for the specified <paramref name="faction"/>, and
        /// then sets <b>Name</b> to the <see cref="DefaultName"/> followed by a space, a hash sign
        /// (#), and the new <b>InstanceCount</b>.
        /// </para><note type="implementnotes">
        /// Calling <b>SetUniqueName</b> repeatedly on the same <paramref name="faction"/> will
        /// increase the faction's <b>InstanceCount</b> for the <b>EntityClass</b> and generate a
        /// new <b>Name</b> with each invocation.</note></remarks>

        internal void SetUniqueName(Faction faction) {
            if (faction == null)
                ThrowHelper.ThrowArgumentNullException("faction");

            // retrieve and increment instance count
            int count = faction.AddInstance(EntityClass);

            // construct unique name with instance count
            Name = String.Format(CultureInfo.InvariantCulture, "{0} #{1}", DefaultName, count + 1);
        }

        #endregion
        #region UpdateModifierMaps

        /// <summary>
        /// Updates all <see cref="Faction"/> modifier maps with the <see cref="Variable"/>
        /// modifiers defined by the <see cref="Entity"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> containing the <see cref="Entity"/>.</param>
        /// <param name="adding">
        /// <c>true</c> to add any matching <see cref="Entity"/> modifier values to the
        /// corresponding modifier maps; <c>false</c> to subtract those <see cref="Entity"/>
        /// modifier values that already exist in the corresponding modifier map.</param>
        /// <remarks><para>
        /// <b>UpdateModifierMaps</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Quit if <see cref="GetLocalSite"/> returns a null reference. Otherwise, the returned
        /// <see cref="World.Site"/> will receive local modifiers in the following steps, and its
        /// neighbors will receive ranged modifiers.
        /// </item><item>
        /// Apply all <see cref="ModifierTarget.OwnerUnits"/> and <see
        /// cref="ModifierTarget.OwnerUnitsRanged"/> modifiers to the modifier maps of the current
        /// <see cref="Owner"/>, if valid.
        /// </item><item>
        /// Apply all <see cref="ModifierTarget.Units"/> and <see
        /// cref="ModifierTarget.UnitsRanged"/> modifiers to the modifier maps of all <see
        /// cref="WorldState.Factions"/> in the specified <paramref name="worldState"/> that differ
        /// from the current <see cref="Owner"/>, which may be a null reference.
        /// </item><item>
        /// Set the <see cref="WorldState.UnitAttributeModifiersChanged"/> flag of the specified
        /// <paramref name="worldState"/> if any <see cref="Faction.UnitAttributeModifiers"/> were
        /// changed.</item></list></remarks>

        internal void UpdateModifierMaps(WorldState worldState, bool adding) {
            Debug.Assert(worldState != null);

            Site site = GetLocalSite(worldState);
            if (site == null) return;

            if (Owner != null)
                UpdateModifierMapsCore(worldState, Owner, site,
                    ModifierTarget.OwnerUnits, ModifierTarget.OwnerUnitsRanged, adding);

            foreach (Faction faction in worldState.Factions)
                if (faction != Owner)
                    UpdateModifierMapsCore(worldState, faction, site,
                        ModifierTarget.Units, ModifierTarget.UnitsRanged, adding);
        }

        #endregion
        #region UpdateModifierMapsCore

        /// <summary>
        /// Updates the modifier maps for the specified <see cref="Faction"/> and <see
        /// cref="ModifierTarget"/> values.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> that contains the specified <paramref name="faction"/>.
        /// </param>
        /// <param name="faction">
        /// The <see cref="Faction"/> whose <see cref="Faction.UnitAttributeModifiers"/> and <see
        /// cref="Faction.UnitResourceModifiers"/> to update.</param>
        /// <param name="site">
        /// The result of <see cref="GetLocalSite"/> for the <see cref="Entity"/>.</param>
        /// <param name="local">
        /// A <see cref="ModifierTarget"/> value indicating the <see cref="Entity"/> modifier values
        /// to apply to the specified <paramref name="site"/>.</param>
        /// <param name="ranged">
        /// A <see cref="ModifierTarget"/> value indicating the <see cref="Entity"/> modifier values
        /// to apply within the entire <see cref="Scenario.EntityClass.ModifierRange"/> around the
        /// specified <paramref name="site"/>.</param>
        /// <param name="adding">
        /// <c>true</c> to add any matching <see cref="Entity"/> modifier values to the
        /// corresponding modifier maps; <c>false</c> to subtract those <see cref="Entity"/>
        /// modifier values that already exist in the corresponding modifier map.</param>
        /// <remarks><para>
        /// <b>UpdateModifierMapsCore</b> is called by <see cref="UpdateModifierMaps"/> for every
        /// <see cref="Faction"/> in the specified <paramref name="worldState"/>. The specified
        /// <paramref name="site"/> always equals the result of <see cref="GetLocalSite"/> and is
        /// never a null reference.
        /// </para><para>
        /// <b>UpdateModifierMapsCore</b> sets the <see
        /// cref="WorldState.UnitAttributeModifiersChanged"/> flag of the specified <paramref
        /// name="worldState"/> to <c>true</c> if the <see cref="Faction.UnitAttributeModifiers"/>
        /// of the specified <paramref name="faction"/> were changed.</para></remarks>

        private void UpdateModifierMapsCore(WorldState worldState, Faction faction,
            Site site, ModifierTarget local, ModifierTarget ranged, bool adding) {

            Debug.Assert(worldState != null);
            Debug.Assert(faction != null);
            Debug.Assert(site != null);

            // adds or removes all matching modifiers to/from map collection
            Action<VariableValueDictionary, VariableList> addModifiers = (target, source) => {
                for (int i = 0; i < source.Count; i++) {
                    var modifier = source[i];
                    int value = modifier.Value;

                    int index = target.IndexOfKey(modifier.Id);
                    if (index >= 0) {
                        int current = target.GetByIndex(index);
                        current = (adding ? current + value : current - value);
                        target.SetByIndex(index, current);
                    }
                    else if (adding)
                        target.Add(modifier.Id, value);
                }
            };

            PointI ps = site.Location;

            // get entity's local modifiers, if any
            var attributeModifiers = AttributeModifiers.GetVariables(local);
            var resourceModifiers = ResourceModifiers.GetVariables(local);

            // add local modifiers to specified site
            if (attributeModifiers.Count > 0) {
                worldState.UnitAttributeModifiersChanged = true;
                addModifiers(faction.UnitAttributeModifiers[ps.X, ps.Y], attributeModifiers);
            }

            if (resourceModifiers.Count > 0)
                addModifiers(faction.UnitResourceModifiers[ps.X, ps.Y], resourceModifiers);

            // get entity's ranged modifiers, if any
            attributeModifiers = AttributeModifiers.GetVariables(ranged);
            resourceModifiers = ResourceModifiers.GetVariables(ranged);

            if (attributeModifiers.Count == 0) {
                if (resourceModifiers.Count == 0) return;
            } else
                worldState.UnitAttributeModifiersChanged = true;

            var mapGrid = Finder.MapGrid;

            if (EntityClass.ModifierRange == 0) {
                // add ranged modifiers to all map sites
                for (int x = 0; x < mapGrid.Size.Width; x++)
                    for (int y = 0; y < mapGrid.Size.Height; y++) {
                        addModifiers(faction.UnitAttributeModifiers[x, y], attributeModifiers);
                        addModifiers(faction.UnitResourceModifiers[x, y], resourceModifiers);
                    }
            } else {
                // add ranged modifiers to sites within range
                var neighbors = mapGrid.GetNeighbors(ps, EntityClass.ModifierRange);
                for (int i = 0; i < neighbors.Count; i++) {
                    PointI p = neighbors[i];
                    addModifiers(faction.UnitAttributeModifiers[p.X, p.Y], attributeModifiers);
                    addModifiers(faction.UnitResourceModifiers[p.X, p.Y], resourceModifiers);
                }
            }
        }

        #endregion
        #region UpdateSelfAndUnitAttributes

        /// <summary>
        /// Applies all modifiers to the <see cref="Attributes"/> of the <see cref="Entity"/> and of
        /// all placed <see cref="World.Site.Units"/> in the current <see cref="WorldState"/>.
        /// </summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <remarks><para>
        /// <b>UpdateSelfAndUnitAttributes</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Call <see cref="UpdateAttributes"/> to update the <see cref="Attributes"/> of the <see
        /// cref="Entity"/> with all current modifiers.
        /// </item><item>
        /// If <see cref="WorldState.UnitAttributeModifiersChanged"/> is <c>true</c>, reset this
        /// flag to <c>false</c> and invoke <see cref="UpdateAttributes"/> on all <see
        /// cref="World.Site.Units"/> placed on any <see cref="World.Site"/> in the current <see
        /// cref="WorldState"/>. This updates their <see cref="Attributes"/> with the current <see
        /// cref="Faction.UnitAttributeModifiers"/> of all <see cref="WorldState.Factions"/>.
        /// </item></list><para>
        /// Step 1 is suppressed if step 2 is going to call <see cref="UpdateAttributes"/> on this
        /// <see cref="Entity"/> anyway.</para></remarks>

        protected internal void UpdateSelfAndUnitAttributes(Command command) {
            WorldState world = command.Context.WorldState;

            if (!world.UnitAttributeModifiersChanged || Category != EntityCategory.Unit)
                UpdateAttributes(command);

            if (world.UnitAttributeModifiersChanged) {
                world.UnitAttributeModifiersChanged = false;

                foreach (Site site in world.Sites)
                    for (int i = 0; i < site.Units.Count; i++)
                        site.Units[i].UpdateAttributes(command);
            }
        }

        #endregion
        #region ValidateOwner

        /// <summary>
        /// Validates the specified <see cref="Faction"/> as the new value of the <see
        /// cref="Owner"/> property.</summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> to validate against the invariants of this <see
        /// cref="Entity"/>.</param>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="faction"/> violates an invariant of this <see cref="Entity"/>.
        /// </exception>
        /// <remarks>
        /// <see cref="Entity"/> does not implement <b>ValidateOwner</b>. Derived classes must
        /// override this method to check for specific invariants.</remarks>

        internal abstract void ValidateOwner(Faction faction);

        #endregion
        #region ValidateSite

        /// <summary>
        /// Validates the specified <see cref="World.Site"/> as the new value of the <see
        /// cref="Site"/> property.</summary>
        /// <param name="site">
        /// The <see cref="World.Site"/> to validate against the invariants of this <see
        /// cref="Entity"/>.</param>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="site"/> violates an invariant of this <see cref="Entity"/>.</exception>
        /// <remarks>
        /// <see cref="Entity"/> does not implement <b>ValidateSite</b>. Derived classes must
        /// override this method to check for specific invariants.</remarks>

        internal abstract void ValidateSite(Site site);

        #endregion
        #endregion
        #region Public Methods
        #region CheckDepletion

        /// <summary>
        /// Checks the <see cref="Entity"/> for depletion of all its <see cref="Resources"/>.
        /// </summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <param name="faction">
        /// The <see cref="Faction"/> to include in any message events. This argument may be a null
        /// reference.</param>
        /// <remarks><para>
        /// <b>CheckDepletion</b> does nothing if the <see
        /// cref="Scenario.EntityClass.ResourceTransfer"/> property of the underlying <see
        /// cref="EntityClass"/> does not equal <see cref="ResourceTransferMode.Delete"/>, or if any
        /// non-zero <see cref="Resources"/> remain.
        /// </para><para>
        /// Otherwise, <b>CheckDepletion</b> invokes <see cref="Delete"/> and creates a message
        /// event for the specified <paramref name="faction"/>, stating that this <see
        /// cref="Entity"/> was completely depleted.
        /// </para><para>
        /// Derived classes may override <b>CheckDepletion</b> to implement different semantics for
        /// depleted <see cref="Resources"/>. Under the default rules, this method is never invoked 
        /// on <see cref="Unit"/> entities.</para></remarks>

        public virtual void CheckDepletion(Command command, Faction faction) {
            if (EntityClass.ResourceTransfer != ResourceTransferMode.Delete)
                return;

            // check if entity has any resources left
            for (int i = 0; i < Resources.Count; i++)
                if (Resources[i].Value != 0) return;

            // delete depleted entity
            Delete(command);

            // create entity depletion event
            string id = (faction == null ? null : faction.Id);
            command.ShowMessage(Global.Strings.EventEntityDepleted, null, id, new string[] { Name });
        }

        #endregion
        #region Delete

        /// <summary>
        /// Deletes the <see cref="Entity"/> from the game.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <remarks><para>
        /// <b>Delete</b> sets the <see cref="Site"/> and <see cref="Owner"/> properties to null
        /// references, and removes the <see cref="Entity"/> from the global <see
        /// cref="WorldState.Entities"/> collection.
        /// </para><para>
        /// Derived classes may override <b>Delete</b> to perform additional actions when an entity
        /// is eliminated.</para></remarks>

        public virtual void Delete(Command command) {
            command.DeleteEntity(Id);
        }

        #endregion
        #region GetDestroyResources

        /// <summary>
        /// Gets the resources gained by executing a <see cref="DestroyCommand"/> on the <see
        /// cref="Entity"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="DestroyCommand"/>.
        /// </param>
        /// <returns>
        /// A <see cref="VariableValueDictionary"/> that maps <see cref="VariableClass.Id"/> strings
        /// of <see cref="VariableClass"/> objects to the amount of each resource gained by
        /// executing the <see cref="DestroyCommand"/>.</returns>
        /// <remarks><para>
        /// <b>GetDestroyResources</b> returns a collection containing one-fifth of each resource
        /// originally required to build the underlying <see cref="EntityClass"/>. These original
        /// build resources are the result of <see cref="Faction.GetBuildResources"/> for the
        /// current <see cref="Owner"/> if valid, and the unmodified <see
        /// cref="Scenario.EntityClass.BuildResources"/> of the <b>EntityClass</b> otherwise.
        /// </para><para>
        /// Derived classes may override <b>GetDestroyResources</b> to implement different semantics
        /// for <see cref="DestroyCommand"/>.
        /// </para><note type="caution">
        /// Do not change any reference parameters or their contents! Create a copy if you need to
        /// change them.</note></remarks>

        public virtual VariableValueDictionary GetDestroyResources(WorldState worldState) {
            Debug.Assert(worldState != null);

            // determine original build resources
            var resources = (Owner == null ?
                new VariableValueDictionary(EntityClass.BuildResources) :
                Owner.GetBuildResources(worldState, EntityClass));

            // reduce all resource values to 1/5
            for (int i = 0; i < resources.Count; i++) {
                int value = resources.GetByIndex(i);
                resources.SetByIndex(i, value / 5);
            }

            return resources;
        }

        #endregion
        #region GetLocalSite

        /// <summary>
        /// Gets the local <see cref="World.Site"/> for the <see cref="Entity"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> containing the <see cref="Entity"/>.</param>
        /// <returns><para>
        /// The <see cref="World.Site"/> that receives any local effects associated with the <see
        /// cref="Entity"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if there is no such <see cref="World.Site"/>.</para></returns>
        /// <remarks><para>
        /// <b>GetLocalSite</b> determines its return value as follows:
        /// </para><list type="number"><item>
        /// If <see cref="Category"/> does not equal <see cref="EntityCategory.Upgrade"/>, return
        /// the current <see cref="Site"/> if valid; otherwise, a null reference.
        /// </item><item>
        /// Otherwise, if the current <see cref="Owner"/> is a null reference, return a null
        /// reference.
        /// </item><item>
        /// Otherwise, if the <see cref="Scenario.EntityClass.ModifierRange"/> of the underlying
        /// <see cref="EntityClass"/> equals zero, return the first element in the <see
        /// cref="WorldState.Sites"/> array of the specified <paramref name="worldState"/>.
        /// </item><item>
        /// Otherwise, return the <see cref="Faction.HomeSite"/> of the current <see cref="Owner"/>
        /// if valid and owned by that faction; otherwise, a null reference.</item></list></remarks>

        public Site GetLocalSite(WorldState worldState) {
            Debug.Assert(worldState != null);

            if (Category != EntityCategory.Upgrade)
                return (Site ?? null);

            if (Owner != null) {
                if (EntityClass.ModifierRange == 0)
                    return worldState.Sites[0, 0];

                var home = worldState.GetSite(Owner.HomeSite);
                if (home != null && home.Owner == Owner)
                    return home;
            }

            return null;
        }

        #endregion
        #region GetVariables

        /// <summary>
        /// Gets a <see cref="VariableList"/> containing all basic values in the specified <see
        /// cref="VariableCategory"/> that are defined by the <see cref="Entity"/>.</summary>
        /// <param name="category">
        /// A <see cref="VariableCategory"/> value indicating which basic values to return.</param>
        /// <returns>
        /// A read-only <see cref="VariableList"/> containing all basic values in the specified
        /// <paramref name="category"/> that are defined by the <see cref="Entity"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="VariableCategory"/> value.
        /// </exception>
        /// <remarks>
        /// <b>GetVariables</b> returns the <see cref="VariableContainer.Variables"/> collection of
        /// the <see cref="Attributes"/>, <see cref="Counters"/>, or <see cref="Resources"/>
        /// container, depending on the specified <paramref name="category"/>.</remarks>

        public VariableList GetVariables(VariableCategory category) {
            switch (category) {

                case VariableCategory.Attribute: return Attributes.Variables;
                case VariableCategory.Counter:   return Counters.Variables;
                case VariableCategory.Resource:  return Resources.Variables;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "category", (int) category, typeof(VariableCategory));
                    return null;
            }
        }

        #endregion
        #region GetVariableModifiers

        /// <summary>
        /// Gets a <see cref="VariableList"/> containing all modifier values in the specified <see
        /// cref="VariableCategory"/> and for the specified <see cref="ModifierTarget"/> that are
        /// defined by the <see cref="Entity"/>.</summary>
        /// <param name="category">
        /// A <see cref="VariableCategory"/> value indicating which modifier values to return.
        /// </param>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating which modifier values to return.</param>
        /// <returns><para>
        /// A read-only <see cref="VariableList"/> containing all modifier values in the specified
        /// <paramref name="category"/> and for the specified <paramref name="target"/> that are
        /// defined by the <see cref="Entity"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if there is no <see cref="VariableList"/> that matches the specified
        /// <paramref name="category"/> and <paramref name="target"/>.</para></returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="VariableCategory"/> value.
        /// </exception>
        /// <remarks>
        /// <b>GetVariables</b> returns a null reference if the specified <paramref
        /// name="category"/> equals <see cref="VariableCategory.Counter"/>, and otherwise the <see
        /// cref="VariableList"/> indicated by specified <paramref name="target"/> of either the
        /// <see cref="AttributeModifiers"/> or the <see cref="ResourceModifiers"/> container.
        /// </remarks>

        public VariableList GetVariableModifiers(VariableCategory category, ModifierTarget target) {
            switch (category) {

                case VariableCategory.Attribute: return AttributeModifiers.GetVariables(target);
                case VariableCategory.Counter:   return null;
                case VariableCategory.Resource:  return ResourceModifiers.GetVariables(target);

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "category", (int) category, typeof(VariableCategory));
                    return null;
            }
        }

        #endregion
        #region MoveToTop

        /// <summary>
        /// Moves the <see cref="Entity"/> to the top of its <see cref="Site"/> stack.</summary>
        /// <returns>
        /// <c>true</c> if <see cref="Site"/> is not a null reference and the index position of the
        /// <see cref="Entity"/> in its <b>Site</b> stack has changed; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <b>MoveToTop</b> moves the <see cref="Entity"/> to the highest index position in its
        /// <see cref="Site"/> stack; that is, the collection returned by <see
        /// cref="World.Site.GetEntities"/> for the current <see cref="Category"/>.</remarks>

        public bool MoveToTop() {

            // do nothing if unplaced
            if (Site == null) return false;

            // retrieve writable entity stack
            EntityList entities = Site.GetWritableEntities(Category);

            // retrieve index within this stack
            int index = entities.IndexOf(this);
            Debug.Assert(index >= 0);

            // do nothing if already on top
            if (index == entities.Count - 1)
                return false;

            // move entity to top of stack
            entities.RemoveAt(index);
            entities.Add(this);

            return true;
        }

        #endregion
        #region OnEntityClassChanged

        /// <summary>
        /// Executes when the <see cref="EntityClass"/> property has changed.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <param name="oldEntityClass">
        /// The value of the <see cref="EntityClass"/> property before it was changed.</param>
        /// <remarks><para>
        /// <b>OnEntityClassChanged</b> calls <see cref="UpdateSelfAndUnitAttributes"/> to update
        /// the <see cref="Attributes"/> of the <see cref="Entity"/> and of all placed units with
        /// any modifiers that were changed due to the new <see cref="EntityClass"/>.
        /// </para><para>
        /// <b>OnEntityClassChanged</b> is called by the <see cref="Command"/> method <see
        /// cref="Command.SetEntityClass"/> after the <see cref="EntityClass"/> property has
        /// changed.
        /// </para><para>
        /// Derived classes may override <b>OnEntityClassChanged</b> to implement different
        /// semantics for a changing <see cref="EntityClass"/>. Overrides should not change the <see
        /// cref="EntityClass"/> of this <see cref="Entity"/> again, either directly or indirectly,
        /// to avoid recursion.</para></remarks>

        public virtual void OnEntityClassChanged(Command command, EntityClass oldEntityClass) {
            UpdateSelfAndUnitAttributes(command);
        }

        #endregion
        #region OnOwnerChanged

        /// <summary>
        /// Executes when the <see cref="Owner"/> property has changed.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <param name="oldOwner">
        /// The value of the <see cref="Owner"/> property before it was changed.</param>
        /// <remarks><para>
        /// <b>OnOwnerChanged</b> calls <see cref="UpdateSelfAndUnitAttributes"/> to update the <see
        /// cref="Attributes"/> of the <see cref="Entity"/> and of all placed units with any
        /// modifiers that were changed due to the new <see cref="Owner"/>.
        /// </para><para>
        /// <b>OnOwnerChanged</b> is called by the <see cref="Command"/> methods <see
        /// cref="Command.DeleteEntity"/>, <see cref="Command.SetEntityOwner"/>, <see
        /// cref="Command.SetSiteOwner"/>, and <see cref="Command.SetSiteUnitOwner"/> after the <see
        /// cref="Owner"/> property has changed. Either the specified <paramref name="oldOwner"/> or
        /// the new <see cref="Owner"/> may be a null reference.
        /// </para><para>
        /// Derived classes may override <b>OnOwnerChanged</b> to implement different semantics for
        /// a changing <see cref="Owner"/>. Overrides should not change the <see cref="Owner"/> of
        /// this <see cref="Entity"/> again, either directly or indirectly, to avoid recursion.
        /// </para></remarks>

        public virtual void OnOwnerChanged(Command command, Faction oldOwner) {
            UpdateSelfAndUnitAttributes(command);
        }

        #endregion
        #region OnSiteChanged

        /// <summary>
        /// Executes when the <see cref="Site"/> property has changed.</summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <param name="oldSite">
        /// The value of the <see cref="Site"/> property before it was changed.</param>
        /// <remarks><para>
        /// <b>OnSiteChanged</b> calls <see cref="UpdateSelfAndUnitAttributes"/> to update the <see
        /// cref="Attributes"/> of the <see cref="Entity"/> and of all placed units with any
        /// modifiers that were changed due to the new <see cref="Site"/>.
        /// </para><para>
        /// <b>OnSiteChanged</b> is called by the <see cref="Command"/> methods <see
        /// cref="Command.DeleteEntity"/> and <see cref="Command.SetEntitySite"/> after the <see
        /// cref="Site"/> property has changed. Either or both of the specified <paramref
        /// name="oldSite"/> and the new <see cref="Site"/> may be a null reference.
        /// </para><para>
        /// Derived classes may override <b>OnSiteChanged</b> to implement different semantics for a
        /// changing <see cref="Site"/>. Overrides should not change the <see cref="Site"/> of this
        /// <see cref="Entity"/> again, either directly or indirectly, to avoid recursion.
        /// </para></remarks>

        public virtual void OnSiteChanged(Command command, Site oldSite) {
            UpdateSelfAndUnitAttributes(command);
        }

        #endregion
        #region OnVariableChanged

        /// <summary>
        /// Executes when a <see cref="Variable"/> value of the <see cref="Entity"/> has changed.
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
        /// <b>OnVariableChanged</b> does nothing if <paramref name="isModifier"/> is <c>false</c>,
        /// or if the <see cref="VariableClass.Category"/> of the specified <paramref
        /// name="variableClass"/> does not equal <see cref="VariableCategory.Attribute"/>.
        /// </para><para>
        /// Otherwise, <b>OnVariableChanged</b> <b>OnSiteChanged</b> calls <see
        /// cref="UpdateSelfAndUnitAttributes"/> to update the <see cref="Attributes"/> of the <see
        /// cref="Entity"/> and of all placed units with the new <see cref="AttributeModifiers"/>.
        /// </para><para>
        /// <b>OnVariableChanged</b> is called by the <see cref="Command"/> methods <see
        /// cref="Command.SetEntityVariable"/> and <see cref="Command.SetEntityVariableModifier"/>
        /// after the indicated <see cref="Variable"/> has been changed to the specified <paramref
        /// name="value"/>.
        /// </para><para>
        /// Derived classes may override <b>OnVariableChanged</b> to perform additional actions when
        /// changing a <see cref="Variable"/>. Overrides should not change the same <see
        /// cref="Variable"/> again, either directly or indirectly, to avoid recursion.
        /// </para><note type="caution">
        /// The specified <paramref name="value"/> is the one that was supplied to the <see
        /// cref="SetEntityVariableInstruction"/> or <see
        /// cref="SetEntityVariableModifierInstruction"/>. The actual new value of the indicated
        /// <see cref="Variable"/> differs if the specified <paramref name="value"/> was outside the
        /// legal range for the <see cref="Variable"/>.</note></remarks>

        public virtual void OnVariableChanged(Command command,
            VariableClass variableClass, int value, bool isModifier) {

            if (isModifier && variableClass.Category == VariableCategory.Attribute)
                UpdateSelfAndUnitAttributes(command);
        }

        #endregion
        #region Place

        /// <summary>
        /// Executes the specified <see cref="PlaceCommand"/>.</summary>
        /// <param name="command">
        /// The <see cref="PlaceCommand"/> to execute.</param>
        /// <remarks><para>
        /// <b>Place</b> assigns the <see cref="Command.Target"/> site of the specified <paramref
        /// name="command"/> to the <see cref="Site"/> property of all elements in the <see
        /// cref="Command.Entities"/> collection of the <paramref name="command"/>.
        /// </para><para>
        /// Derived classes may override <b>Place</b> to implement different semantics for <see
        /// cref="PlaceCommand"/>.
        /// </para><para>
        /// The command's <b>Entities</b> collection always contains at least one element, and the
        /// <b>Value</b> of the first element always equals this <see cref="Entity"/>.
        /// </para></remarks>

        public virtual void Place(PlaceCommand command) {
            Debug.Assert(command.Entities[0].Value == this);

            // place all entities on target site
            foreach (EntityReference entity in command.Entities)
                command.SetEntitySite(entity.Id, command.Target.Location);
        }

        #endregion
        #region ToEntityTemplate

        /// <summary>
        /// Returns an <see cref="Scenario.EntityTemplate"/> that contains all representable data of
        /// the <see cref="Entity"/>.</summary>
        /// <returns>
        /// The value of the <see cref="EntityTemplate"/> property if it is not a null reference;
        /// otherwise, a new <see cref="Scenario.EntityTemplate"/> object based on the current data
        /// of the <see cref="Entity"/>.</returns>
        /// <remarks><para>
        /// The <see cref="FrameOffset"/> of a new <see cref="Scenario.EntityTemplate"/> is zero if
        /// the current <see cref="DisplayClass"/> is animated.
        /// </para><para>
        /// Otherwise, the new <see cref="FrameOffset"/> is restricted to the <see
        /// cref="Scenario.EntityClass.FrameCount"/> of the underlying <see cref="EntityClass"/>
        /// which may be different from the current <see cref="DisplayClass"/>.
        /// </para><para>
        /// Any variable changes of the <see cref="Entity"/> compared to the underlying <see
        /// cref="EntityClass"/> are also exported to a new <see cref="Scenario.EntityTemplate"/>.
        /// </para></remarks>

        public EntityTemplate ToEntityTemplate() {
            Debug.Assert(!ApplicationInfo.IsEditor || EntityTemplate != null);

            /*
             * Entities created by Hexkit Editor always have a valid EntityTemplate.
             *
             * In that case, we could not use ExportChanges to reconstruct variable
             * values anyway, because the EntityClassCache that provides EntityClass
             * variables only returns empty collections in editing mode.
             */

            if (EntityTemplate != null)
                return EntityTemplate;

            int offset = 0;
            if (DisplayClass.ImageAnimation == AnimationMode.None) {
                offset = Math.Abs(FrameOffset);
                if (EntityClass.FrameCount > 0)
                    offset %= EntityClass.FrameCount;
            }

            EntityTemplate template = new EntityTemplate(EntityClass, offset, InstanceName);

            Attributes.ExportChanges(EntityClass.Attributes, template.Attributes);
            AttributeModifiers.ExportChanges(EntityClass.AttributeModifiers, template.AttributeModifiers);
            Counters.ExportChanges(EntityClass.Counters, template.Counters);
            Resources.ExportChanges(EntityClass.Resources, template.Resources);
            ResourceModifiers.ExportChanges(EntityClass.ResourceModifiers, template.ResourceModifiers);

            return template;
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="Entity"/>.</summary>
        /// <returns>
        /// The value of the <see cref="Name"/> property, if it is not an empty string; otherwise,
        /// the value of the <see cref="Id"/> property.</returns>

        public override string ToString() {
            return (Name.Length == 0 ? Id : Name);
        }

        #endregion
        #region UpdateAttributes

        /// <summary>
        /// Applies all modifiers to the <see cref="Attributes"/> of the <see cref="Entity"/>.
        /// </summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <remarks><para>
        /// <b>UpdateAttributes</b> updates the values in the <see cref="Attributes"/> collection to
        /// account for any applicable modifiers, as follows:
        /// </para><list type="number"><item>
        /// Quit if <see cref="IsModifiable"/> is <c>false</c>.
        /// </item><item>
        /// Reset all <see cref="Attributes"/> to their <see cref="Variable.InitialValue"/>.
        /// </item><item>
        /// Add the <see cref="ModifierTarget.Self"/> modifier of each matching <see
        /// cref="AttributeModifiers"/> element defined by the <see cref="Entity"/>.
        /// </item><item>
        /// For units, also add each matching <see cref="Faction.UnitAttributeModifiers"/> element
        /// defined by the <see cref="Owner"/> of the <see cref="Unit"/>.</item></list></remarks>

        public void UpdateAttributes(Command command) {

            // check if we should apply modifiers
            if (!IsModifiable) return;

            // units add aggregate modifiers for current site
            VariableValueDictionary modifiers = (this is Unit ?
                Owner.UnitAttributeModifiers[Site.Location.X, Site.Location.Y] : null);

            // nothing to do if no modifiers defined
            if (modifiers == null && AttributeModifiers.Self.Count == 0)
                return;

            // add matching modifiers to all attributes
            for (int i = 0; i < Attributes.Count; i++) {
                Variable attribute = Attributes[i];
                string id = attribute.Id;

                // get total modifier for current attribute
                int modifier = 0;
                if (modifiers != null) modifiers.TryGetValue(id, out modifier);
                modifier += AttributeModifiers.GetValue(id, ModifierTarget.Self);

                // update current attribute value if changed
                int value = attribute.InitialValue + modifier;
                if (value != attribute.Value)
                    command.SetEntityVariable(Id, id, value);
            }
        }

        #endregion
        #region UpdateResources

        /// <summary>
        /// Applies all modifiers to the <see cref="Resources"/> of the <see cref="Entity"/>.
        /// </summary>
        /// <param name="command">
        /// The <see cref="Command"/> being executed.</param>
        /// <remarks><para>
        /// <b>UpdateResources</b> updates the values in the <see cref="Resources"/> collection to
        /// account for any applicable modifiers, as follows:
        /// </para><list type="number"><item>
        /// Quit if <see cref="IsModifiable"/> is <c>false</c>.
        /// </item><item>
        /// Obtain the current <see cref="Variable.Value"/> of each <see cref="Resources"/> element.
        /// </item><item>
        /// Add the <see cref="ModifierTarget.Self"/> modifier of each matching <see
        /// cref="ResourceModifiers"/> element of the <see cref="Entity"/>.
        /// </item><item>
        /// For units, also add each matching <see cref="Faction.UnitResourceModifiers"/> element
        /// defined by the <see cref="Owner"/> of the <see cref="Unit"/>.
        /// </item><item>
        /// For units, also call <see cref="Unit.AddSiteResources"/> to add any transferable <see
        /// cref="Resources"/> available in the same <see cref="Site"/>.</item></list></remarks>

        public void UpdateResources(Command command) {

            // check if we should apply modifiers
            if (!IsModifiable) return;

            // units add aggregate modifiers for current site
            Unit unit = this as Unit;
            VariableValueDictionary modifiers = (unit != null ?
                Owner.UnitResourceModifiers[Site.Location.X, Site.Location.Y] : null);

            // nothing to do if no modifiers defined
            if (unit == null && ResourceModifiers.Self.Count == 0)
                return;

            // add matching modifiers to all resources
            for (int i = 0; i < Resources.Count; i++) {
                Variable resource = Resources[i];
                string id = resource.Id;

                // get total modifier for current resource
                int modifier = 0;
                if (modifiers != null) modifiers.TryGetValue(id, out modifier);
                modifier += ResourceModifiers.GetValue(id, ModifierTarget.Self);

                // update current resource value if changed
                if (modifier != 0)
                    command.SetEntityVariable(Id, id, resource.Value + modifier);
            }

            // units add matching local resources
            if (unit != null) unit.AddSiteResources(command);
        }

        #endregion
        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="Entity"/> object that is a deep copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new <see cref="Entity"/> object that is a deep copy of the current instance.</returns>
        /// <remarks>
        /// <see cref="Entity"/> does not implement <b>Clone</b>. Derived classes must override this
        /// method to call the <see cref="Entity(Entity)"/> copy constructor with this instance, and
        /// then perform any additional copying operations required by the derived class.</remarks>

        public abstract object Clone();

        #endregion
        #region IKeyedValue Members

        /// <summary>
        /// Gets the identifier of the <see cref="Entity"/>.</summary>
        /// <value>
        /// The value of the <see cref="Id"/> property.</value>

        string IKeyedValue<String>.Key {
            [DebuggerStepThrough]
            get { return Id; }
        }

        #endregion
        #region IValuable Members

        /// <summary>
        /// Gets the valuation of the <see cref="Entity"/>.</summary>
        /// <value>
        /// A <see cref="Double"/> value in the standard interval [0,1], indicating the desirability
        /// of the <see cref="Entity"/> to computer players. Higher values indicate greater
        /// desirability.</value>
        /// <remarks><para>
        /// <b>Valuation</b> performs the following steps to evaluate the <see cref="Entity"/>:
        /// </para><list type="number"><item>
        /// Start with the <see cref="Scenario.EntityClass.Valuation"/> of the underlying <see
        /// cref="EntityClass"/>.
        /// </item><item>
        /// For each element in the <see cref="Resources"/> collection, multiply the valuation with
        /// the relative magnitude of the <b>Resources</b> value compared to its <see
        /// cref="Variable.InitialValue"/>.
        /// </item><item>
        /// Restrict the result to a maximum value of one.</item></list></remarks>

        public double Valuation {
            get {
                // retrieve entity class valuation
                double value = EntityClass.Valuation;

                // restrict by current resources
                for (int i = 0; i < Resources.Count; i++) {
                    Variable resource = Resources[i];

                    // multiply by quotient if values differ
                    if (resource.Value != resource.InitialValue)
                        value *= resource.GetRelativeMagnitude(resource.InitialValue);
                }

                // restrict value to standard interval
                if (value > 1.0) value = 1.0;
                Debug.Assert(value >= 0.0);

                return value;
            }
        }

        #endregion
    }
}
