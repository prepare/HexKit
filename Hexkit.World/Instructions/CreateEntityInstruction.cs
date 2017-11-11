using System;

using Tektosyne;
using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Creates a new <see cref="Entity"/>.</summary>
    /// <remarks><para>
    /// <b>CreateEntityInstruction</b> creates a new <see cref="Entity"/> object based on a specific
    /// <see cref="EntityClass"/>. The new <see cref="Entity"/> receives a unique identifier within
    /// the current <see cref="WorldState"/>.
    /// </para><para>
    /// <b>CreateEntityInstruction</b> is serialized to the XML element "CreateEntity" defined in
    /// <see cref="FilePaths.SessionSchema"/>.</para></remarks>

    public sealed class CreateEntityInstruction: Instruction {
        #region CreateEntityInstruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="CreateEntityInstruction"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateEntityInstruction"/> class with
        /// default properties.</summary>

        internal CreateEntityInstruction(): base() { }

        #endregion
        #region CreateEntityInstruction(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateEntityInstruction"/> class with the
        /// specified <see cref="EntityClass"/> identifier.</summary>
        /// <param name="classId">
        /// The initial value for the <see cref="Instruction.Id"/> property, indicating the <see
        /// cref="EntityClass.Id"/> string of the <see cref="EntityClass"/> to instantiate.</param>

        internal CreateEntityInstruction(string classId): base(classId) { }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see
        /// cref="CreateEntityInstruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="CreateEntityInstruction"/> has changed the
        /// data of the specified <paramref name="worldState"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="CreateEntityInstruction"/> contains data that is invalid with respect to
        /// the specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// <b>Execute</b> performs the following actions:
        /// </para><list type="number"><item>
        /// Call <see cref="Instruction.GetEntityClass"/> to obtain the <see cref="EntityClass"/>
        /// identified by the <see cref="Instruction.Id"/> property.
        /// </item><item>
        /// Invoke <see cref="IRulesFactory.CreateEntity"/> on the current rule script's <see
        /// cref="RuleScript.Factory"/> with the resulting <b>EntityClass</b>.
        /// </item><item>
        /// Invoke <see cref="Entity.SetUniqueIdentifier"/> on the resulting <see cref="Entity"/>.
        /// </item><item>
        /// Set the <see cref="InstructionResults.Entity"/> property of the <see
        /// cref="Instruction.Results"/> cache to the newly created <b>Entity</b>.
        /// </item><item>
        /// Start a new <see cref="EntityHistory"/> for the newly created <b>Entity</b>.
        /// </item></list><para>
        /// <b>Execute</b> always returns <c>true</c>.</para></remarks>

        internal override bool Execute(WorldState worldState) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            // GetEntityClass throws exception for invalid Id
            EntityClass entityClass = GetEntityClass(Id);

            // use rule script factory to create entity
            IRulesFactory factory = (IRulesFactory) MasterSection.Instance.Rules.Factory;
            Entity entity = factory.CreateEntity(entityClass);

            // create unique identifier and return entity
            entity.SetUniqueIdentifier(worldState);
            if (Results != null) Results.Entity = entity;

            // start history for new entity
            worldState.History.AddEntity(worldState, entity);

            return true;
        }

        #endregion
    }
}
