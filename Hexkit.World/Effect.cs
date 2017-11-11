using System;
using System.Diagnostics;

using Tektosyne.Collections;
using Hexkit.Scenario;

namespace Hexkit.World {
    #region Type Aliases

    using EntityList = KeyedList<String, Entity>;

    #endregion

    /// <summary>
    /// Represents an effect that appears on a <see cref="Site"/>.</summary>
    /// <remarks><para>
    /// <b>Effect</b> may be overridden by the rule script to define actual semantics for effects.
    /// </para><para>
    /// The default semantics of effects are identical to those of terrains, except that effects are
    /// always placed on the map. <b>Effect</b> is a separate class mainly because effects must be
    /// drawn on top of all other local entities, whereas terrains are drawn below all other local
    /// entities.</para></remarks>

    public class Effect: Entity {
        #region Effect(Effect)

        /// <overloads>
        /// Initializes a new instance of the <see cref="Effect"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="Effect"/> class that is a deep copy of the
        /// specified instance.</summary>
        /// <param name="effect">
        /// The <see cref="Effect"/> object whose properties should be copied to the new instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="effect"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="effect"/>. Please refer to <see cref="Entity(Entity)"/> for
        /// details.</remarks>

        protected Effect(Effect effect): base(effect) { }

        #endregion
        #region Effect(EffectClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="Effect"/> class based on the specified <see
        /// cref="Scenario.EffectClass"/>.</summary>
        /// <param name="effectClass">
        /// The initial value for the <see cref="EffectClass"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="effectClass"/> is a null reference.</exception>
        /// <remarks>
        /// Clients should use factory methods to instantiate the <see cref="Effect"/> class, either
        /// <see cref="Entity.CreateEntity"/> or an equivalent method defined by the rule script.
        /// </remarks>

        public Effect(EffectClass effectClass): base(effectClass) { }

        #endregion
        #region EffectClass

        /// <summary>
        /// Gets the scenario class of the <see cref="Effect"/> object.</summary>
        /// <value>
        /// The <see cref="Scenario.EffectClass"/> on which the <see cref="Effect"/> object is
        /// based.</value>
        /// <remarks>
        /// <b>EffectClass</b> returns the value of the <see cref="Entity.EntityClass"/> property,
        /// cast to type <see cref="Scenario.EffectClass"/> for convenience.</remarks>

        public EffectClass EffectClass {
            [DebuggerStepThrough]
            get { return (EffectClass) EntityClass; }
        }

        #endregion
        #region ValidateOwner

        /// <summary>
        /// Validates the specified <see cref="Faction"/> as the new value of the <see
        /// cref="Entity.Owner"/> property.</summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> to validate against the invariants of the <see cref="Effect"/>
        /// class.</param>
        /// <exception cref="InvalidCommandException">
        /// <see cref="Entity.Site"/> is valid, and its <see cref="Site.Owner"/> differs from the
        /// specified <paramref name="faction"/>. Effects must share the owner of their site.
        /// </exception>
        /// <remarks>
        /// <b>ValidateOwner</b> does nothing if validation succeeds.</remarks>

        internal override sealed void ValidateOwner(Faction faction) {
            if (Site != null && faction != Site.Owner)
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.ErrorOwnerEffectConflict, Id);
        }

        #endregion
        #region ValidateSite

        /// <summary>
        /// Validates the specified <see cref="Site"/> as the new value of the <see
        /// cref="Entity.Site"/> property.</summary>
        /// <param name="site">
        /// The <see cref="Site"/> to validate against the invariants of the <see cref="Effect"/>
        /// class.</param>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="site"/> is a null reference. Effects must always be placed.</exception>
        /// <remarks>
        /// <b>ValidateSite</b> does nothing if validation succeeds.</remarks>

        internal override sealed void ValidateSite(Site site) {
            if (site == null)
                InvalidCommandException.ThrowNewWithFormat(Global.Strings.ErrorSiteEffectNone, Id);
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="Effect"/> object that is a deep copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new <see cref="Effect"/> object that is a deep copy of the current instance.</returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="Effect(Effect)"/> copy constructor with this <see
        /// cref="Effect"/> object.</remarks>

        public override object Clone() {
            return new Effect(this);
        }

        #endregion
    }
}
