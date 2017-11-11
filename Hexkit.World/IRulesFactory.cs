using System;
using Hexkit.Scenario;

namespace Hexkit.World {

    /// <summary>
    /// Provides factory methods for customized game rules.</summary>
    /// <remarks><para>
    /// Scenario rule scripts can derive customized classes from the <see cref="Entity"/> and <see
    /// cref="Faction"/> classes to modify the behavior of entities and factions during a game. This
    /// allows the rule script to change and enhance the default game rules for a specific scenario.
    /// </para><para>
    /// Implementing customized game rules via polymorphism has two major advantages:
    /// <b>Hexkit.World</b> does not have to distinguish between default and custom rules; and there
    /// is no reflection speed penalty for explicitly calling into a dynamically created assembly.
    /// </para><para>
    /// <b>IRulesFactory</b> provides two factory methods that let the <see cref="WorldState"/>
    /// class instantiate rule script classes. This interface cannot be defined in the
    /// <b>Hexkit.Scenario</b> assembly where the rule script is loaded and compiled, as it requires
    /// two <b>Hexkit.World</b> types that are not known to that assembly.</para></remarks>

    public interface IRulesFactory: IDisposable {
        #region CreateEntity

        /// <summary>
        /// Creates a new <see cref="Entity"/> object based on the specified <see
        /// cref="EntityClass"/>.</summary>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> to instantiate.</param>
        /// <returns>
        /// A new <see cref="Entity"/> object based on the specified <paramref name="entityClass"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entityClass"/> is a null reference.</exception>
        /// <remarks>
        /// <b>CreateEntity</b> conforms to the <see cref="CreateEntityCallback"/> delegate. Clients
        /// may override the constructor of types derived from <see cref="Entity"/> to create
        /// customized <b>Entity</b> objects.</remarks>

        Entity CreateEntity(EntityClass entityClass);

        #endregion
        #region CreateFaction

        /// <summary>
        /// Creates a new <see cref="Faction"/> object based on the specified <see
        /// cref="FactionClass"/>.</summary>
        /// <param name="factionClass">
        /// The <see cref="FactionClass"/> to instantiate.</param>
        /// <returns>
        /// A new <see cref="Faction"/> object based on the specified <paramref
        /// name="factionClass"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="factionClass"/> is a null reference.</exception>
        /// <remarks>
        /// <b>CreateFaction</b> conforms to the <see cref="CreateFactionCallback"/> delegate.
        /// Clients may override the <see cref="Faction"/> constructor to create customized
        /// <b>Faction</b> objects.</remarks>

        Faction CreateFaction(FactionClass factionClass);

        #endregion
        #region Initialize

        /// <summary>
        /// Initializes script-specific data based on the initial <see cref="WorldState"/>.
        /// </summary>
        /// <param name="worldState">
        /// The fully created <see cref="WorldState"/> at the start of the game.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>Initialize</b> is called once at the start of a game session, just after the initial
        /// <see cref="WorldState"/> has been fully created for the first time. Although the initial
        /// <b>WorldState</b> may be subsequently recreated during turn replays, <b>Initialize</b>
        /// is never invoked twice on the same <see cref="IRulesFactory"/> instance.
        /// </para><para>
        /// The specified <paramref name="worldState"/> is the result of all <see
        /// cref="CreateFaction"/> and <see cref="CreateEntity"/> calls required by the scenario,
        /// but before the first game command or HCL instruction has been executed. This allows the
        /// rule script to establish private data based on a complete <b>WorldState</b> that has not
        /// yet been modified by replayed HCL instructions.</para></remarks>

        void Initialize(WorldState worldState);

        #endregion
    }
}
