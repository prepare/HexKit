using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

using Tektosyne;
using Tektosyne.Windows;
using Hexkit.World;

namespace Hexkit.Players {

    /// <summary>
    /// Specifies the arguments for a computer player background thread.</summary>
    /// <remarks><para>
    /// <b>ComputerThreadArgs</b> encapsulates all required and optional arguments to start computer
    /// player calculations in a background thread using the <see cref="ComputerThread"/> class.
    /// </para><para>
    /// <b>ComputerThreadArgs</b> provides a <see cref="TaskEvents"/> object to allow communication
    /// with the foreground thread. Clients should handle the <see cref="TaskEvents.TaskComplete"/>,
    /// <see cref="TaskEvents.TaskException"/>, and <see cref="TaskEvents.TaskMessage"/> events to
    /// process thread termination, thread exception, and message display, respectively.
    /// </para><para>
    /// The <b>TaskComplete</b> event is always raised just before the <b>ComputerThread</b>
    /// terminates, even if a <b>TaskException</b> event has already been raised. The
    /// <b>TaskException</b> event is <em>not</em> raised if a <see cref="ThreadAbortException"/>
    /// occurs.</para></remarks>

    public class ComputerThreadArgs {
        #region ComputerThreadArgs(...)

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputerThreadArgs"/> class.</summary>
        /// <param name="dispatcher">
        /// The initial value for the <see cref="TaskEvents.Dispatcher"/> property of the associated
        /// <see cref="Events"/> object.</param>
        /// <param name="worldState">
        /// The initial value for the <see cref="WorldState"/> property.</param>
        /// <param name="options">
        /// The initial value for the <see cref="Options"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dispatcher"/>, <paramref name="options"/>, or <paramref
        /// name="worldState"/> is a null reference.</exception>

        public ComputerThreadArgs(Dispatcher dispatcher,
            WorldState worldState, AlgorithmOptions options) {

            if (dispatcher == null)
                ThrowHelper.ThrowArgumentNullException("dispatcher");
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (options == null)
                ThrowHelper.ThrowArgumentNullException("options");

            this._events = new TaskEvents(dispatcher);
            this._worldState = worldState;
            this._options = options;
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly TaskEvents _events;
        private readonly AlgorithmOptions _options;
        private readonly WorldState _worldState;

        #endregion
        #region Events

        /// <summary>
        /// Gets a <see cref="TaskEvents"/> object that communicates the status of the background
        /// thread to the application's foreground thread.</summary>
        /// <value>
        /// A <see cref="TaskEvents"/> object whose event handler calls are marshalled to the
        /// foreground thread using the associated <see cref="TaskEvents.Dispatcher"/>.</value>
        /// <remarks>
        /// <b>Events</b> never returns a null reference. This property never changes once the
        /// object has been constructed.</remarks>

        public TaskEvents Events {
            [DebuggerStepThrough]
            get { return this._events; }
        }

        #endregion
        #region Options

        /// <summary>
        /// Gets the algorithm to run on the background thread and its optional settings.</summary>
        /// <value>
        /// An <see cref="AlgorithmOptions"/> object containing the algorithm whose <see
        /// cref="Algorithm.FindBestWorld"/> method will be executed on the background thread, and
        /// any optional settings for the algorithm.</value>
        /// <remarks>
        /// <b>Options</b> never returns a null reference. This property never changes once the
        /// object has been constructed.</remarks>

        public AlgorithmOptions Options {
            [DebuggerStepThrough]
            get { return this._options; }
        }

        #endregion
        #region WorldState

        /// <summary>
        /// Gets the <see cref="World.WorldState"/> on which the background thread operates.
        /// </summary>
        /// <value>
        /// The <see cref="World.WorldState"/> that represents the game world at the beginning of
        /// the turn.</value>
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
