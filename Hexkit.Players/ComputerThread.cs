using System;
using System.Diagnostics;
using System.Threading;

using Tektosyne;
using Hexkit.World;

namespace Hexkit.Players {

    /// <summary>
    /// Runs a computer player algorithm on a background thread.</summary>
    /// <remarks><para>
    /// <b>ComputerThread</b> executes the <see cref="Algorithm.FindBestWorld"/> method of an <see
    /// cref="Algorithm"/> on a background thread. The startup data for the background thread is
    /// supplied by a <see cref="ComputerThreadArgs"/> object.
    /// </para><para>
    /// An instance of the <b>ComputerThread</b> class can only control a single background thread,
    /// but multiple instances may be active simultaneously.</para></remarks>

    public class ComputerThread: IDisposable {
        #region Private Fields

        // active background thread
        private Thread _thread;
        private ComputerThreadArgs _args;

        // was thread terminated by Abort?
        private volatile bool _isAborted;

        // property backers
        private volatile bool _isDisposed;

        #endregion
        #region BestWorld

        /// <summary>
        /// Gets the best possible end-of-turn <see cref="WorldState"/> found by the last successful
        /// computer calculation.</summary>
        /// <value><para>
        /// The value of the <see cref="Algorithm.BestWorld"/> property of the <see
        /// cref="AlgorithmOptions.Algorithm"/> supplied to the last call to <see cref="Start"/>.
        /// </para><para>-or-</para><para>
        /// A null reference if the background thread raised an exception, or if <b>Start</b> has
        /// not been called yet.</para></value>

        public WorldState BestWorld {
            [DebuggerStepThrough]
            get {
                return (this._args == null ? null :
                    this._args.Options.Algorithm.BestWorld);
            }
        }

        #endregion
        #region IsDisposed

        /// <summary>
        /// Gets a value indicating whether the <see cref="ComputerThread"/> object has been
        /// disposed of.</summary>
        /// <value>
        /// <c>true</c> if this <see cref="ComputerThread"/> object has been disposed of; otherwise,
        /// <c>false</c>. The default is <c>false</c>.</value>
        /// <remarks><para>
        /// <b>IsDisposed</b> never changes back to <c>false</c> once it has been set to
        /// <c>true</c>.
        /// </para><para>
        /// The background thread checks this property to determine whether to invoke the callback
        /// methods specified by the current <see cref="ComputerThreadArgs"/> object.
        /// </para></remarks>

        public bool IsDisposed {
            [DebuggerStepThrough]
            get { return this._isDisposed; }
        }

        #endregion
        #region Start

        /// <summary>
        /// Starts a new background thread with the specified <see cref="ComputerThreadArgs"/>.
        /// </summary>
        /// <param name="args">
        /// A <see cref="ComputerThreadArgs"/> object containing startup data for the new background
        /// thread.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="args"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>Start</b> calls <see cref="Stop"/> to stop any active background thread, and then
        /// starts a new background thread that runs the <see cref="Algorithm.FindBestWorld"/>
        /// method of the algorithm indicated by <see cref="ComputerThreadArgs.Options"/> on the
        /// indicated <see cref="ComputerThreadArgs.WorldState"/>.
        /// </para><para>
        /// The background thread will use the remaining properties of the specified <paramref
        /// name="args"/> object to communicate with the application's foreground thread. The result
        /// is available through the <see cref="BestWorld"/> property after the background thread
        /// has terminated.</para></remarks>

        public void Start(ComputerThreadArgs args) {
            if (args == null)
                ThrowHelper.ThrowArgumentNullException("args");

            // clear any previous results
            args.Options.Algorithm.ClearBestWorld();

            Stop();
            this._args = args;
            this._isAborted = false;

            this._thread = new Thread(new ThreadStart(ThreadMain));
            this._thread.IsBackground = true;
            this._thread.Name = this._args.Options.Algorithm.Id;

            this._thread.Start();
        }

        #endregion
        #region Stop

        /// <summary>
        /// Stops the background thread.</summary>
        /// <returns>
        /// <c>true</c> if the background thread was stopped as requested; <c>false</c> if no
        /// background thread was active, or if it was already terminating.</returns>
        /// <remarks><para>
        /// <b>Stop</b> calls <see cref="Thread.Abort"/> and <see cref="Thread.Join"/> on the
        /// background thread if one is active.
        /// </para><para>
        /// <b>Stop</b> returns <c>true</c> if an active background thread was stopped by a <see
        /// cref="ThreadAbortException"/>, and <c>false</c> in any other case.</para></remarks>

        public bool Stop() {

            // fail if never started or not running
            if (this._thread == null || !this._thread.IsAlive)
                return false;

            try {
                this._thread.Abort();
            }
            catch (ThreadStateException) {
                // may occur if thread was suspended
            }

            this._thread.Join();
            return this._isAborted;
        }

        #endregion
        #region ThreadMain

        /// <summary>
        /// Runs computer player calculations on a background thread.</summary>
        /// <remarks>
        /// <b>ThreadMain</b> is the entry point for all background threads started by <see
        /// cref="Start"/>. Please refer to that method for further details.</remarks>

        private void ThreadMain() {
            try {
                // show algorithm name
                this._args.Events.OnTaskMessage(this,
                    Global.Strings.StatusAlgorithmExecuting,
                    this._args.Options.Algorithm.Name);

                // set BestWorld property
                this._args.Options.Algorithm.FindBestWorld(
                    this._args.WorldState, this._args.Options, this._args.Events);
            }
            catch (Exception e) {
                // clear BestWorld property
                this._args.Options.Algorithm.ClearBestWorld();

                // check for explicit Abort call
                if (e is ThreadAbortException) {
                    this._isAborted = true;
                } else if (!IsDisposed) {
                    // notify client of unexpected exception
                    this._args.Events.OnTaskException(this, e);
                }
            }
            finally {
                if (!IsDisposed) {
                    // try to clear message display
                    this._args.Events.OnTaskMessage(this, null);

                    // notify client that calculation stopped
                    this._args.Events.OnTaskComplete(this);
                }
            }
        }

        #endregion
        #region IDisposable Members

        /// <summary>
        /// Releases all resources used by the <see cref="ComputerThread"/> object.</summary>
        /// <remarks>
        /// <b>Dispose</b> sets <see cref="IsDisposed"/> to <c>true</c> and calls <see cref="Stop"/>
        /// to terminate the background thread if one is still active.</remarks>

        public void Dispose() {
            this._isDisposed = true;
            Stop();
        }

        #endregion
    }
}
