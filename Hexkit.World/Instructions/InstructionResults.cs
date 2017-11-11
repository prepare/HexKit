using System;
using System.Diagnostics;

using Hexkit.Scenario;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Provides the results of the execution of an HCL instruction.</summary>
    /// <remarks><para>
    /// <b>InstructionResults</b> is a simple data container for various object references that were
    /// obtained or created during the execution of an HCL instruction. The documentation for each
    /// instruction specified which references are set, if any.
    /// </para><para>
    /// When used for newly created objects, <b>InstructionResults</b> allows HCL instructions to
    /// communicate back results to the caller. When used for instruction arguments,
    /// <b>InstructionResults</b> relieves clients from the need to recreate object references from
    /// the originally supplied identifiers or coordinates.</para></remarks>

    public class InstructionResults {
        #region Private Fields

        // property backers
        private Entity _entity;
        private EntityClass _entityClass;
        private Faction _faction;
        private Site _site;
        private VariableClass _variableClass;

        #endregion
        #region Entity

        /// <summary>
        /// Gets or sets the <see cref="World.Entity"/> that was affected by the <see
        /// cref="Instruction"/>.</summary>
        /// <value>
        /// The <see cref="World.Entity"/> that was affected by the <see cref="Instruction"/>. The
        /// default is a null reference.</value>

        public Entity Entity {
            [DebuggerStepThrough]
            get { return this._entity; }
            [DebuggerStepThrough]
            set { this._entity = value; }
        }

        #endregion
        #region EntityClass

        /// <summary>
        /// Gets or sets the <see cref="Scenario.EntityClass"/> that was affected by the <see
        /// cref="Instruction"/>.</summary>
        /// <value>
        /// The <see cref="Scenario.EntityClass"/> that was affected by the <see
        /// cref="Instruction"/>. The default is a null reference.</value>

        public EntityClass EntityClass {
            [DebuggerStepThrough]
            get { return this._entityClass; }
            [DebuggerStepThrough]
            set { this._entityClass = value; }
        }

        #endregion
        #region Faction

        /// <summary>
        /// Gets or sets the <see cref="World.Faction"/> that was affected by the <see
        /// cref="Instruction"/>.</summary>
        /// <value>
        /// The <see cref="World.Faction"/> that was affected by the <see cref="Instruction"/>. The
        /// default is a null reference.</value>

        public Faction Faction {
            [DebuggerStepThrough]
            get { return this._faction; }
            [DebuggerStepThrough]
            set { this._faction = value; }
        }

        #endregion
        #region Site

        /// <summary>
        /// Gets or sets the <see cref="World.Site"/> that was affected by the <see
        /// cref="Instruction"/>.</summary>
        /// <value>
        /// The <see cref="World.Site"/> that was affected by the <see cref="Instruction"/>. The
        /// default is a null reference.</value>

        public Site Site {
            [DebuggerStepThrough]
            get { return this._site; }
            [DebuggerStepThrough]
            set { this._site = value; }
        }

        #endregion
        #region VariableClass

        /// <summary>
        /// Gets or sets the <see cref="Scenario.VariableClass"/> whose instance value was affected
        /// by the <see cref="Instruction"/>.</summary>
        /// <value>
        /// The <see cref="Scenario.VariableClass"/> whose instance value was affected by the <see
        /// cref="Instruction"/>. The default is a null reference.</value>

        public VariableClass VariableClass {
            [DebuggerStepThrough]
            get { return this._variableClass; }
            [DebuggerStepThrough]
            set { this._variableClass = value; }
        }

        #endregion
    }
}
