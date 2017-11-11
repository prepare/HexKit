using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Represents a class of upgrade entities.</summary>
    /// <remarks>
    /// <b>UpgradeClass</b> corresponds to the complex XML type "entityClass" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>, as this class does not require any specific data.
    /// Instances are serialized to the XML element "upgrade".</remarks>

    public sealed class UpgradeClass: EntityClass {
        #region UpgradeClass()

        /// <overloads>
        /// Initializes a new instance of the <see cref="UpgradeClass"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="UpgradeClass"/> class with default
        /// properties.</summary>
        /// <remarks>
        /// The <see cref="EntityClass.Category"/> property is initialized to a value of <see
        /// cref="EntityCategory.Upgrade"/>.</remarks>

        internal UpgradeClass(): base("", EntityCategory.Upgrade) { }

        #endregion
        #region UpgradeClass(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="UpgradeClass"/> class with the specified
        /// identifier.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="EntityClass.Id"/> property.</param>
        /// <remarks>
        /// The <see cref="EntityClass.Category"/> property is initialized to a value of <see
        /// cref="EntityCategory.Upgrade"/>.</remarks>

        public UpgradeClass(string id): base(id, EntityCategory.Upgrade) { }

        #endregion
        #region UpgradeClass(UpgradeClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="UpgradeClass"/> class with property values
        /// copied from the specified instance.</summary>
        /// <param name="upgrade">
        /// The <see cref="UpgradeClass"/> instance whose property values are copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="upgrade"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="upgrade"/>, including a deep copy of all mutable objects that
        /// are owned by the <paramref name="upgrade"/>.</remarks>

        public UpgradeClass(UpgradeClass upgrade): base(upgrade) { }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="UpgradeClass"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="UpgradeClass"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="UpgradeClass(UpgradeClass)"/> copy constructor with
        /// this <see cref="UpgradeClass"/> object.</remarks>

        public override object Clone() {
            return new UpgradeClass(this);
        }

        #endregion
        #region XmlSerializable Members

        /// <summary>
        /// The name of the XML element associated with the <see cref="UpgradeClass"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "upgrade", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="UpgradeClass"/> class.</remarks>

        public const string ConstXmlName = "upgrade";

        #endregion
    }
}
