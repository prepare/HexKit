using System;
using System.Diagnostics;

using Tektosyne;

namespace Hexkit.World.Commands {

    /// <summary>
    /// Provides the context in which a command is executed.</summary>
    /// <remarks><para>
    /// <b>ExecutionContext</b> provides all necessary external data for the generation and
    /// execution of the HCL program associated with a <see cref="Command"/>.
    /// </para><para>
    /// This includes the <see cref="WorldState"/> on which a command is executed, as well as
    /// optional callback methods to handle command queueing and event display.</para></remarks>

    public class ExecutionContext {
        #region ExecutionContext(...)

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionContext"/> class.</summary>
        /// <param name="worldState">
        /// The initial value for the <see cref="WorldState"/> property.</param>
        /// <param name="queueCommand">
        /// The initial value for the <see cref="QueueCommand"/> property.</param>
        /// <param name="showEvent">
        /// The initial value for the <see cref="ShowEvent"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>

        public ExecutionContext(WorldState worldState,
            QueueCommandCallback queueCommand, ShowEventCallback showEvent) {

            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");

            this._worldState = worldState;
            this._queueCommand = queueCommand;
            this._showEvent = showEvent;
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly WorldState _worldState;
        private readonly QueueCommandCallback _queueCommand;
        private readonly ShowEventCallback _showEvent;

        #endregion
        #region QueueCommand

        /// <summary>
        /// Gets the method that enqueues a specified <see cref="Command"/> for execution after the
        /// current command.</summary>
        /// <value>
        /// The <see cref="QueueCommandCallback"/> method supplied to the constructor of the <see
        /// cref="ExecutionContext"/>.</value>
        /// <remarks>
        /// <b>QueueCommand</b> may return a null reference when an existing HCL program is being
        /// replayed. This property never changes once the object has been constructed.</remarks>

        public QueueCommandCallback QueueCommand {
            [DebuggerStepThrough]
            get { return this._queueCommand; }
        }

        #endregion
        #region ShowEvent

        /// <summary>
        /// Gets the method that handles event display during <see cref="Command"/> execution.
        /// </summary>
        /// <value>
        /// The <see cref="ShowEventCallback"/> method supplied to the constructor of the <see
        /// cref="ExecutionContext"/>.</value>
        /// <remarks>
        /// <b>ShowEvent</b> may return a null reference to indicate that event display should be
        /// suppressed. This property never changes once the object has been constructed.</remarks>

        public ShowEventCallback ShowEvent {
            [DebuggerStepThrough]
            get { return this._showEvent; }
        }

        #endregion
        #region WorldState

        /// <summary>
        /// Gets the <see cref="World.WorldState"/> on which a <see cref="Command"/> is executed.
        /// </summary>
        /// <value>
        /// The <see cref="World.WorldState"/> supplied to the constructor of the <see
        /// cref="ExecutionContext"/>.</value>
        /// <remarks>
        /// <b>WorldState</b> never returns a null reference. This property never changes once the
        /// object has been constructed.</remarks>

        public WorldState WorldState {
            [DebuggerStepThrough]
            get { return this._worldState; }
        }

        #endregion
    }
}
