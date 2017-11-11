using System;
using System.Diagnostics;
using System.Windows.Threading;

using Tektosyne;
using Hexkit.Players;
using Hexkit.World;

namespace Hexkit.Game {

    /// <summary>
    /// Synchronizes user actions with concurrent game actions.</summary>
    /// <remarks><para>
    /// Certain user actions, such as changing the player setup or saving the game, require that the
    /// current <see cref="Session.WorldState"/> remains unchanged while they are executed. This
    /// condition has the following requirements:
    /// </para><list type="number"><item>
    /// Any ongoing execution of a human player <see cref="SessionState.Command"/> must be allowed
    /// to complete. Map view events will be suppressed to speed up the execution.
    /// </item><item>
    /// Any ongoing <see cref="SessionState.Replay"/> must be stopped since the replayed commands
    /// continually change the <see cref="Session.WorldState"/>.
    /// </item><item>
    /// If the automatic replay of a computer player's turn was stopped, the automatic activation of
    /// the next faction must be suspended.
    /// </item><item>
    /// If a computer player is still performing turn calculations, the automatic retrieval and
    /// replaying of its completed turn must be suspended.
    /// </item><item>
    /// If the <see cref="AsyncAction.Count"/> of outstanding asynchronous operations is greater
    /// than zero, those operations must be allowed to complete.
    /// </item></list><para>
    /// <b>UserAction</b> ensures that an arbitrary user-initiated <see cref="Action"/>, which may
    /// overlap with these automatic game actions, is executed only when these requirements are met.
    /// Since human player commands and history replays cannot be stopped immediately, the <see
    /// cref="UserAction.TryRun"/> method schedules the <see cref="Action"/> for automatic later
    /// execution in such cases.
    /// </para><para>
    /// <see cref="UserAction.TryRun"/> also stores any pertinent <see cref="Session"/> data prior
    /// to executing the <see cref="Action"/>, and resumes any suspended automatic methods when
    /// execution has finished. Clients may explicitly call <see cref="UserAction.Clear"/> to
    /// prevent this resumption.</para></remarks>

    public static class UserAction {
        #region Private Fields

        // property backers
        private static DispatcherTimer _retryTimer;

        // action to execute
        private static Action _action;

        // session at the time TryRun was called
        private static WeakReference _oldSession;

        // did we stop a computer turn replay?
        private static bool _computerReplay;

        // did we pause computer turn calculations?
        private static bool _computerThread;

        #endregion
        #region Private Members
        #region RetryTimer

        /// <summary>
        /// Gets the timer for retrying a stored user <see cref="Action"/>.</summary>
        /// <value>
        /// The <see cref="DispatcherTimer"/> whose <see cref="DispatcherTimer.Tick"/> event 
        /// periodically retries calling <see cref="Run"/>.</value>
        /// <remarks>
        /// <b>RetryTimer</b> is started by <see cref="TryRun"/> when a requested user <see
        /// cref="Action"/> cannot be executed immediately because <see cref="IsSessionBusy"/> is
        /// <c>true</c>.</remarks>

        private static DispatcherTimer RetryTimer {
            [DebuggerStepThrough]
            get {
                if (_retryTimer == null) {
                    _retryTimer = new DispatcherTimer(DispatcherPriority.Background);
                    _retryTimer.IsEnabled = false;
                    _retryTimer.Interval = TimeSpan.FromMilliseconds(10);
                    _retryTimer.Tick += OnRetryTick;
                }

                return _retryTimer;
            }
        }

        #endregion
        #region OnRetryTick

        /// <summary>
        /// Handles the <see cref="DispatcherTimer.Tick"/> event for the <see cref="RetryTimer"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="DispatcherTimer"/> sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnRetryTick</b> calls <see cref="Clear"/> if <see cref="IsBusy"/> is <c>false</c>.
        /// </para><para>
        /// Otherwise, <b>OnRetryTick</b> disables the <see cref="RetryTimer"/> and executes the
        /// stored user <see cref="Action"/> if <see cref="IsSessionBusy"/> is already <c>false</c>.
        /// </para></remarks>

        private static void OnRetryTick(object sender, EventArgs args) {

            if (!IsBusy)
                Clear();
            else if (!IsSessionBusy) {
                RetryTimer.IsEnabled = false;
                Run();
            }
        }

        #endregion
        #region Run

        /// <summary>
        /// Executes the stored user <see cref="Action"/> and resumes suspended methods.</summary>
        /// <remarks><para>
        /// <b>Run</b> executes the user <see cref="Action"/> requested in the last call to <see
        /// cref="TryRun"/>, if any.
        /// </para><para>
        /// If <see cref="Clear"/> was not called, and the current <see cref="Session"/> instance is
        /// valid and unchanged, and the <see cref="MainWindow.ComputerEndTurn"/> or <see
        /// cref="MainWindow.OnComputerComplete"/> methods were suspended by <see cref="TryRun"/>,
        /// <b>Run</b> then calls either method as appropriate.</para></remarks>

        private static void Run() {

            // execute stored action, if any
            if (_action != null) _action();

            // check if Clear has been called
            if (!IsBusy) return;

            // get suspended session data, if any
            Session oldSession = (_oldSession == null ? null : (Session) _oldSession.Target);
            bool computerReplay = _computerReplay, computerThread = _computerThread;
            Clear();

            // do nothing if game already closed or changed
            if (Session.State == SessionState.Invalid || Session.Instance != oldSession)
                return;

            // resume suspended methods
            if (computerReplay)
                MainWindow.ComputerEndTurn();
            else {
                MainWindow window = MainWindow.Instance;
                if (computerThread && window.IsComputerStopped)
                    window.OnComputerComplete(PlayerManager.Instance.ComputerThread, EventArgs.Empty);
            }
        }

        #endregion
        #endregion
        #region IsBusy

        /// <summary>
        /// Gets a value indicating whether a user <see cref="Action"/> is executing or scheduled
        /// for execution.</summary>
        /// <value>
        /// <c>true</c> if an <see cref="Action"/> is executing or scheduled for execution, and <see
        /// cref="Clear"/> was not yet called; otherwise, <c>false</c>. The default is <c>false</c>.
        /// </value>
        /// <remarks><para>
        /// Any method whose automatic execution might conflict with the concurrent execution of a
        /// user <see cref="Action"/> should check <b>IsBusy</b> and return immediately if its value
        /// is <c>true</c>. <see cref="UserAction"/> will eventually call that method again.
        /// </para><note type="implementnotes">
        /// Such methods currently include <see cref="MainWindow.ComputerEndTurn"/> and <see
        /// cref="MainWindow.OnComputerComplete"/> in the <see cref="MainWindow"/> class.
        /// </note></remarks>

        public static bool IsBusy { get; private set; }

        #endregion
        #region IsSessionBusy

        /// <summary>
        /// Gets a value indicating whether the current <see cref="Session"/> is busy with an
        /// automatic action that prevents executing a user <see cref="Action"/>.</summary>
        /// <value>
        /// <c>true</c> if the current session <see cref="Session.State"/> equals <see
        /// cref="SessionState.Command"/> or <see cref="SessionState.Replay"/>, or the current <see
        /// cref="AsyncAction.Count"/> of outstanding asynchronous actions is greater than zero;
        /// otherwise, <c>false</c>.</value>
        /// <remarks>
        /// <see cref="TryRun"/> schedules a requested user <see cref="Action"/> for later execution
        /// if <see cref="IsSessionBusy"/> is <c>true</c>. The action will be executed at some point
        /// after <see cref="IsSessionBusy"/> has changed to <c>false</c>.</remarks>

        public static bool IsSessionBusy {
            get {
                return (Session.State == SessionState.Command
                    || Session.State == SessionState.Replay
                    || AsyncAction.Count > 0);
            }
        }

        #endregion
        #region Clear

        /// <summary>
        /// Clears all data recorded by <see cref="TryRun"/>.</summary>
        /// <remarks><para>
        /// <b>Clear</b> sets <see cref="IsBusy"/> to <c>false</c>, deletes any stored user <see
        /// cref="Action"/>, and clears all <see cref="Session"/> data that was recorded by the last
        /// successful <see cref="TryRun"/> call. The stored user <see cref="Action"/> itself may
        /// call <see cref="Clear"/> to prevent a resumption of interrupted methods.
        /// </para><note type="implementnotes">
        /// Call this method when the player controlling the <see cref="WorldState.ActiveFaction"/>
        /// changes while the <see cref="Action"/> was executing.</note></remarks>

        public static void Clear() {

            IsBusy = false;
            RetryTimer.IsEnabled = false;

            var session = Session.Instance;
            if (session != null) SessionExecutor.AbortSignal.Reset();

            _action = null;
            _oldSession = null;
            _computerReplay = false;
            _computerThread = false;
        }

        #endregion
        #region TryRun

        /// <summary>
        /// Attempts to execute the specified user <see cref="Action"/>.</summary>
        /// <param name="action">
        /// The user <see cref="Action"/> to execute.</param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="action"/> was either executed or scheduled
        /// for later execution; <c>false</c> if <paramref name="action"/> was discarded.</returns>
        /// <remarks><para>
        /// <b>TryRun</b> immediately fails if <see cref="IsBusy"/> is <c>true</c>, indicating that
        /// another <see cref="Action"/> is already executing or pending execution. In that case,
        /// the specified <paramref name="action"/> is discarded and will never execute.
        /// </para><para>
        /// Otherwise, <b>TryRun</b> sets <see cref="IsBusy"/> to <c>true</c> and succeeds. The
        /// specified <paramref name="action"/> is executed immediately if <see
        /// cref="IsSessionBusy"/> is currently <c>false</c>. In that case, <see cref="IsBusy"/>
        /// will be reset to <c>false</c> before <b>TryRun</b> returns.
        /// </para><para>
        /// Otherwise, <b>TryRun</b> and schedules the specified <paramref name="action"/> for
        /// future execution. <see cref="IsBusy"/> remains <c>true</c> upon return, and is reset to
        /// <c>false</c> either when the <paramref name="action"/> has been executed or when <see
        /// cref="Clear"/> has been called, whichever comes first.
        /// </para><para>
        /// On success, <b>TryRun</b> also records the current session <see cref="Session.State"/>
        /// and any impending automatic method calls that should be suspended and resumed.
        /// Resumption occurs after the <paramref name="action"/> has been executed, and may be
        /// suppressed by calling <see cref="Clear"/>.</para></remarks>

        public static bool TryRun(Action action) {
            if (action == null)
                ThrowHelper.ThrowArgumentNullException("action");

            // fail if already busy
            if (IsBusy) return false;

            IsBusy = true;
            _action = action;

            // remember current session
            Session session = Session.Instance;
            _oldSession = new WeakReference(session);

            switch (Session.State) {

                case SessionState.Command:
                    // temporarily disable command delays
                    SessionExecutor.AbortSignal.Set();

                    // retry when command has executed
                    RetryTimer.IsEnabled = true;
                    break;

                case SessionState.Replay:
                    // remember computer turn was replaying
                    _computerReplay = session.Replay.IsComputerTurn;

                    // retry when replay has stopped
                    session.Replay.RequestState(ReplayState.Stop);
                    RetryTimer.IsEnabled = true;
                    break;

                case SessionState.Computer:
                    // remember if computer thread was running
                    _computerThread = true;
                    goto default;

                default:
                    if (AsyncAction.Count > 0) {
                        // retry when operations are finished
                        RetryTimer.IsEnabled = true;
                    } else {
                        Debug.Assert(!IsSessionBusy);
                        Run(); // execute immediately
                    }
                    break;
            }

            return true;
        }

        #endregion
    }
}
