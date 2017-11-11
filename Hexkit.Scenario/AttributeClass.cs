using System;
using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Represents a class of attribute variables.</summary>
    /// <remarks>
    /// <b>AttributeClass</b> corresponds to the complex XML type "attributeClass" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>. Instances are serialized to the XML element "attribute".
    /// </remarks>

    public sealed class AttributeClass: VariableClass {
        #region AttributeClass()

        /// <overloads>
        /// Initializes a new instance of the <see cref="AttributeClass"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeClass"/> class with default
        /// properties.</summary>
        /// <remarks>
        /// The <see cref="VariableClass.Category"/> property is initialized to a value of <see
        /// cref="VariableCategory.Attribute"/>.</remarks>

        internal AttributeClass(): base("", VariableCategory.Attribute) { }

        #endregion
        #region AttributeClass(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeClass"/> class with the specified
        /// identifier.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="VariableClass.Id"/> property.</param>
        /// <remarks>
        /// The <see cref="VariableClass.Category"/> property is initialized to a value of <see
        /// cref="VariableCategory.Attribute"/>.</remarks>

        public AttributeClass(string id): base(id, VariableCategory.Attribute) { }

        #endregion
        #region AttributeClass(String, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeClass"/> class with the specified
        /// identifier and display name.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="VariableClass.Id"/> property.</param>
        /// <param name="name">
        /// The initial value for the <see cref="VariableClass.Name"/> property.</param>
        /// <remarks>
        /// The <see cref="VariableClass.Category"/> property is initialized to a value of <see
        /// cref="VariableCategory.Attribute"/>.</remarks>

        public AttributeClass(string id, string name):
            base(id, VariableCategory.Attribute, name) { }

        #endregion
        #region AttributeClass(AttributeClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeClass"/> class with property
        /// values copied from the specified instance.</summary>
        /// <param name="attribute">
        /// The <see cref="AttributeClass"/> instance whose property values are copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="attribute"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="attribute"/>, including a deep copy of all mutable objects
        /// that are owned by the <paramref name="attribute"/>.</remarks>

        public AttributeClass(AttributeClass attribute): base(attribute) { }

        #endregion
        #region StandardAttackRange

        /// <summary>
        /// A read-only <see cref="AttributeClass"/> that represents the standard attack range
        /// attribute for units.</summary>
        /// <remarks><para>
        /// <b>StandardAttackRange</b> represents a pseudo-attribute for user interfaces and does
        /// not appear in the <see cref="VariableSection.Attributes"/> collection.
        /// </para><para>
        /// All <b>StandardAttackRange</b> properties have default values, except for <see
        /// cref="VariableClass.Id"/> which returns "standard-attack-range", and <see
        /// cref="VariableClass.Name"/> which returns the localized string "Standard Attack Range".
        /// </para></remarks>

        public static readonly AttributeClass StandardAttackRange =
            new AttributeClass("standard-attack-range", Global.Strings.LabelStandardAttackRange);

        #endregion
        #region StandardDifficulty

        /// <summary>
        /// A read-only <see cref="AttributeClass"/> that represents the standard difficulty
        /// attribute for terrains.</summary>
        /// <remarks><para>
        /// <b>StandardDifficulty</b> represents a pseudo-attribute for user interfaces and does not
        /// appear in the <see cref="VariableSection.Attributes"/> collection.
        /// </para><para>
        /// All <b>StandardDifficulty</b> properties have default values, except for <see
        /// cref="VariableClass.Id"/> which returns "standard-difficulty", and <see
        /// cref="VariableClass.Name"/> which returns the localized string "Standard Difficulty".
        /// </para></remarks>

        public static readonly AttributeClass StandardDifficulty =
            new AttributeClass("standard-difficulty", Global.Strings.LabelStandardDifficulty);

        #endregion
        #region StandardElevation

        /// <summary>
        /// A read-only <see cref="AttributeClass"/> that represents the standard elevation
        /// attribute for terrains.</summary>
        /// <remarks><para>
        /// <b>StandardElevation</b> represents a pseudo-attribute for user interfaces and does not
        /// appear in the <see cref="VariableSection.Attributes"/> collection.
        /// </para><para>
        /// All <b>StandardElevation</b> properties have default values, except for <see
        /// cref="VariableClass.Id"/> which returns "standard-elevation", and <see
        /// cref="VariableClass.Name"/> which returns the localized string "Standard Elevation".
        /// </para></remarks>

        public static readonly AttributeClass StandardElevation =
            new AttributeClass("standard-elevation", Global.Strings.LabelStandardElevation);

        #endregion
        #region StandardMovement

        /// <summary>
        /// A read-only <see cref="AttributeClass"/> that represents the standard movement attribute
        /// for units.</summary>
        /// <remarks><para>
        /// <b>StandardMovement</b> represents a pseudo-attribute for user interfaces and does not
        /// appear in the <see cref="VariableSection.Attributes"/> collection.
        /// </para><para>
        /// All <b>StandardMovement</b> properties have default values, except for <see
        /// cref="VariableClass.Id"/> which returns "standard-movement", and <see
        /// cref="VariableClass.Name"/> which returns the localized string "Standard Movement".
        /// </para></remarks>

        public static readonly AttributeClass StandardMovement =
            new AttributeClass("standard-movement", Global.Strings.LabelStandardMovement);

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="AttributeClass"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="AttributeClass"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="AttributeClass(AttributeClass)"/> copy constructor
        /// with this <see cref="AttributeClass"/> object.</remarks>

        public override object Clone() {
            return new AttributeClass(this);
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="AttributeClass"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "attribute", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="AttributeClass"/> class.</remarks>

        public const string ConstXmlName = "attribute";

        #endregion
        #endregion
    }
}
