using System;

using Hexkit.Scenario;
using Hexkit.World.Commands;
using Hexkit.World.Instructions;

namespace Hexkit.World {
    #region CreateEntityCallback

    /// <summary>
    /// Represents the method that will create a new <see cref="Entity"/> object based on the
    /// specified <see cref="EntityClass"/>.</summary>
    /// <param name="entityClass">
    /// The <see cref="EntityClass"/> to instantiate.</param>
    /// <returns>
    /// A new <see cref="Entity"/> object based on the specified <paramref name="entityClass"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="entityClass"/> is a null reference.</exception>
    /// <remarks>
    /// <b>CreateEntityCallback</b> can be used to represent the default <see
    /// cref="Entity.CreateEntity"/> method, as well as any <see cref="IRulesFactory.CreateEntity"/>
    /// method accessed through the <see cref="IRulesFactory"/> interface.</remarks>

    public delegate Entity CreateEntityCallback(EntityClass entityClass);

    #endregion
    #region CreateFactionCallback

    /// <summary>
    /// Represents the method that will create a new <see cref="Faction"/> object based on the
    /// specified <see cref="FactionClass"/>.</summary>
    /// <param name="factionClass">
    /// The <see cref="FactionClass"/> to instantiate.</param>
    /// <returns>
    /// A new <see cref="Faction"/> object based on the specified <paramref name="factionClass"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="factionClass"/> is a null reference.</exception>
    /// <remarks>
    /// <b>CreateFactionCallback</b> can be used to represent the default <see
    /// cref="Faction.CreateFaction"/> method, as well as any <see
    /// cref="IRulesFactory.CreateFaction"/> method accessed through the <see cref="IRulesFactory"/>
    /// interface.</remarks>

    public delegate Faction CreateFactionCallback(FactionClass factionClass);

    #endregion
    #region GetPlayerSettings

    /// <summary>
    /// Represents the method that will retrieve the current <see cref="PlayerSettings"/> for the
    /// specified <see cref="Faction"/>.</summary>
    /// <param name="faction">
    /// The <see cref="Faction"/> whose <see cref="PlayerSettings"/> to retrieve.</param>
    /// <returns>
    /// The current <see cref="PlayerSettings"/> for the <see cref="Faction"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="faction"/> is a null reference.</exception>

    public delegate PlayerSettings GetPlayerSettingsCallback(Faction faction);

    #endregion
    #region QueueCommandCallback

    /// <summary>
    /// Represents the method that will enqueue the specified <see cref="Command"/> for execution
    /// after the current command.</summary>
    /// <param name="command">
    /// The <see cref="Command"/> to enqueue.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="command"/> is a null reference.</exception>
    /// <exception cref="InvalidCommandException">
    /// <paramref name="command"/> is a <see cref="BeginTurnCommand"/> or an <see
    /// cref="EndTurnCommand"/>.</exception>
    /// <remarks>
    /// <b>QueueCommandCallback</b> represents the method that will add the specified <paramref
    /// name="command"/> to the <see cref="CommandExecutor.QueuedCommands"/> collection of a <see
    /// cref="CommandExecutor"/>.</remarks>

    public delegate void QueueCommandCallback(Command command);

    #endregion
    #region ShowEventCallback

    /// <summary>
    /// Represents the method that will display the event encoded by the specified HCL <see
    /// cref="Instruction"/>.</summary>
    /// <param name="instruction">
    /// The <see cref="Instruction"/> that encodes the event to display.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="instruction"/> is a null reference.</exception>
    /// <remarks>
    /// <b>ShowEventCallback</b> represents the method that will be invoked to display the message
    /// or map view event encoded by the specified <paramref name="instruction"/>. This method may
    /// have various effects or do nothing, depending on the context in which the HCL instructions
    /// are executed.</remarks>

    public delegate void ShowEventCallback(Instruction instruction);

    #endregion
}
