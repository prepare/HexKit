using System;
using System.Diagnostics;

using Tektosyne.Collections;
using Hexkit.Scenario;
using Hexkit.World.Commands;

namespace Hexkit.World {
    #region Type Aliases

    using EntityList = KeyedList<String, Entity>;

    #endregion

    /// <summary>
    /// Represents an upgrade that belongs to a <see cref="Faction"/>.</summary>
    /// <remarks><para>
    /// <b>Upgrade</b> may be overridden by the rule script to define actual upgrade semantics.
    /// </para><para>
    /// The default semantics require that an <b>Upgrade</b> resides in a faction's inventory. 
    /// Upgrades exert a global effect on their owner's resources and units.</para></remarks>

    public class Upgrade: Entity {
        #region Upgrade(Upgrade)

        /// <overloads>
        /// Initializes a new instance of the <see cref="Upgrade"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="Upgrade"/> class that is a deep copy of the
        /// specified instance.</summary>
        /// <param name="upgrade">
        /// The <see cref="Upgrade"/> object whose properties should be copied to the new instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="upgrade"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="upgrade"/>. Please refer to <see cref="Entity(Entity)"/> for
        /// details.</remarks>

        protected Upgrade(Upgrade upgrade): base(upgrade) { }

        #endregion
        #region Upgrade(UpgradeClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="Upgrade"/> class based on the specified
        /// <see cref="Scenario.UpgradeClass"/>.</summary>
        /// <param name="upgradeClass">
        /// The initial value for the <see cref="UpgradeClass"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="upgradeClass"/> is a null reference.</exception>
        /// <remarks>
        /// Clients should use factory methods to instantiate the <see cref="Upgrade"/> class,
        /// either <see cref="Entity.CreateEntity"/> or an equivalent method defined by the rule
        /// script.</remarks>

        public Upgrade(UpgradeClass upgradeClass): base(upgradeClass) { }

        #endregion
        #region UpgradeClass

        /// <summary>
        /// Gets the scenario class of the <see cref="Upgrade"/>.</summary>
        /// <value>
        /// The <see cref="Scenario.UpgradeClass"/> on which the <see cref="Upgrade"/> is based.
        /// </value>
        /// <remarks>
        /// <b>UpgradeClass</b> returns the value of the <see cref="Entity.EntityClass"/> property,
        /// cast to type <see cref="Scenario.UpgradeClass"/> for convenience.</remarks>

        public UpgradeClass UpgradeClass {
            [DebuggerStepThrough]
            get { return (UpgradeClass) EntityClass; }
        }

        #endregion
        #region ValidateOwner

        /// <summary>
        /// Validates the specified <see cref="Faction"/> as the new value of the <see
        /// cref="Entity.Owner"/> property.</summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> to validate against the invariants of the <see
        /// cref="Upgrade"/> class.</param>
        /// <exception cref="InvalidCommandException">
        /// The specified <paramref name="faction"/> is a null reference. Upgrades must be owned.
        /// </exception>
        /// <remarks>
        /// <b>ValidateOwner</b> does nothing if validation succeeds.</remarks>

        internal override sealed void ValidateOwner(Faction faction) {
            if (faction == null)
                InvalidCommandException.ThrowNewWithFormat(Global.Strings.ErrorOwnerUpgradeNone, Id);
        }

        #endregion
        #region ValidateSite

        /// <summary>
        /// Validates the specified <see cref="Site"/> as the new value of the <see
        /// cref="Entity.Site"/> property.</summary>
        /// <param name="site">
        /// The <see cref="Site"/> to validate against the invariants of the <see cref="Upgrade"/>
        /// class.</param>
        /// <exception cref="InvalidCommandException">
        /// The specified <paramref name="site"/> is not a null reference. Upgrades cannot be
        /// placed.</exception>
        /// <remarks>
        /// <b>ValidateSite</b> does nothing if validation succeeds.</remarks>

        internal override sealed void ValidateSite(Site site) {
            if (site != null)
                InvalidCommandException.ThrowNewWithFormat(Global.Strings.ErrorSiteUpgradeValid, Id);
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="Upgrade"/> object that is a deep copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new <see cref="Upgrade"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="Upgrade(Upgrade)"/> copy constructor with this <see
        /// cref="Upgrade"/> object.</remarks>

        public override object Clone() {
            return new Upgrade(this);
        }

        #endregion
    }
}
