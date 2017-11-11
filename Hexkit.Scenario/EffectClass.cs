using System;
using System.Xml;

using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Represents a class of effect entities.</summary>
    /// <remarks>
    /// <b>EffectClass</b> corresponds to the complex XML type "entityClass" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>, as this class does not require any specific data.
    /// Instances are serialized to the XML element "effect".</remarks>

    public sealed class EffectClass: EntityClass {
        #region EffectClass()

        /// <overloads>
        /// Initializes a new instance of the <see cref="EffectClass"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="EffectClass"/> class with default
        /// properties.</summary>
        /// <remarks>
        /// The <see cref="EntityClass.Category"/> property is initialized to a value of <see
        /// cref="EntityCategory.Effect"/>.</remarks>

        internal EffectClass(): base("", EntityCategory.Effect) { }

        #endregion
        #region EffectClass(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="EffectClass"/> class with the specified
        /// identifier.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="EntityClass.Id"/> property.</param>
        /// <remarks>
        /// The <see cref="EntityClass.Category"/> property is initialized to a value of <see
        /// cref="EntityCategory.Effect"/>.</remarks>

        public EffectClass(string id): base(id, EntityCategory.Effect) { }

        #endregion
        #region EffectClass(EffectClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="EffectClass"/> class with property values
        /// copied from the specified instance.</summary>
        /// <param name="effect">
        /// The <see cref="EffectClass"/> instance whose property values are copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="effect"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="effect"/>, including a deep copy of all mutable objects that
        /// are owned by the <paramref name="effect"/>.</remarks>

        public EffectClass(EffectClass effect): base(effect) { }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="EffectClass"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="EffectClass"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="EffectClass(EffectClass)"/> copy constructor with this
        /// <see cref="EffectClass"/> object.</remarks>

        public override object Clone() {
            return new EffectClass(this);
        }

        #endregion
        #region XmlSerializable Members

        /// <summary>
        /// The name of the XML element associated with the <see cref="EffectClass"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "effect", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see cref="EffectClass"/>
        /// class.</remarks>

        public const string ConstXmlName = "effect";

        #endregion
    }
}
