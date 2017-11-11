using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using Tektosyne;

namespace Hexkit.Game {

    /// <summary>
    /// Manages asynchronous operations running on the <see cref="ThreadPool"/>.</summary>
    /// <remarks><para>
    /// <b>AsyncAction</b> provides methods to initiate asynchronous operations on the <see
    /// cref="ThreadPool"/>, a <see cref="AsyncAction.Count"/> property to keep track of outstanding
    /// asynchronous operations, and wrappers to marshal calls to the <see cref="Dispatcher"/>
    /// thread of the current <see cref="Application"/> instance.
    /// </para><para>
    /// Use <b>AsyncAction</b> to run <see cref="SessionExecutor"/> and <see cref="ReplayManager"/>
    /// methods on a background thread. The <see cref="UserAction"/> class monitors the <see
    /// cref="AsyncAction.Count"/> property to ensure safe execution of user actions.
    /// </para></remarks>

    public static class AsyncAction {
        #region Private Fields

        // property backers
        private static volatile int _count;

        #endregion
        #region Count

        /// <summary>
        /// Gets the number of outstanding <see cref="Run"/> and <see cref="BeginRun"/> operations.
        /// </summary>
        /// <value>
        /// The number of asynchronous operations that were started by <see cref="Run"/> or <see
        /// cref="BeginRun"/> and have not yet completed. The default is zero.</value>
        /// <remarks>
        /// <b>Count</b> is automatically incremented by <see cref="Run"/> and <see
        /// cref="BeginRun"/>; also automatically decremented by <see cref="Run"/>; and explicitly
        /// decremented by <see cref="EndRun"/>.</remarks>

        public static int Count {
            [DebuggerStepThrough]
            get { return _count; }
        }

        #endregion
        #region BeginInvoke

        /// <summary>
        /// Asynchronously executes the specified <see cref="Action"/> on the <see
        /// cref="Dispatcher"/> thread of the current <see cref="Application"/>.</summary>
        /// <param name="action">
        /// The <see cref="Action"/> to execute.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="action"/> is a null reference.</exception>
        /// <remarks>
        /// <b>BeginInvoke</b> calls <see cref="Dispatcher.BeginInvoke"/> on the <see
        /// cref="Dispatcher"/> of the current <see cref="Application"/> instance with the specified
        /// <paramref name="action"/>.</remarks>

        public static void BeginInvoke(Action action) {
            if (action == null)
                ThrowHelper.ThrowArgumentNullException("action");

            Application.Current.Dispatcher.BeginInvoke(action);
        }

        #endregion
        #region BeginRun

        /// <summary>
        /// Asynchronously executes the specified <see cref="Action"/>, without decrementing <see
        /// cref="Count"/> after execution.</summary>
        /// <param name="action">
        /// The <see cref="Action"/> to execute.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="action"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>BeginRun</b> atomically increments <see cref="Count"/> before queuing the specified
        /// <paramref name="action"/> on the <see cref="ThreadPool"/>, but does <em>not</em>
        /// decrement <see cref="Count"/> after the <paramref name="action"/> was executed. The
        /// specified <paramref name="action"/> must do this explicitly by calling <see
        /// cref="EndRun"/> before returning.
        /// </para><para>
        /// <b>BeginRun</b> is intended for situations when the asynchronous <paramref
        /// name="action"/> ends with a <see cref="BeginInvoke"/> call, which must then call <see
        /// cref="EndRun"/> just before returning. Using <see cref="Run"/> would require changing
        /// this call to a less efficient <see cref="Invoke"/> call instead.</para></remarks>

        public static void BeginRun(Action action) {
            if (action == null)
                ThrowHelper.ThrowArgumentNullException("action");

#pragma warning disable 0420
            Interlocked.Increment(ref _count);
            ThreadPool.QueueUserWorkItem(delegate { action(); });
#pragma warning restore 0420
        }

        #endregion
        #region EndRun

        /// <summary>
        /// Atomically decrements <see cref="Count"/>.</summary>
        /// <remarks>
        /// <b>EndRun</b> must be called explicitly within an <see cref="Action"/> that was started
        /// by <see cref="BeginRun"/>. The <see cref="Run"/> method automatically calls
        /// <b>EndRun</b> after its <see cref="Action"/> has executed.</remarks>

        public static void EndRun() {
#pragma warning disable 0420
            Interlocked.Decrement(ref _count);
#pragma warning restore 0420
        }

        #endregion
        #region Invoke

        /// <summary>
        /// Synchronously executes the specified <see cref="Action"/> on the <see
        /// cref="Dispatcher"/> thread of the current <see cref="Application"/>.</summary>
        /// <param name="action">
        /// The <see cref="Action"/> to execute.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="action"/> is a null reference.</exception>
        /// <remarks>
        /// <b>Invoke</b> calls <see cref="Dispatcher.Invoke"/> on the <see cref="Dispatcher"/> of
        /// the current <see cref="Application"/> instance with the specified <paramref
        /// name="action"/>.</remarks>

        public static void Invoke(Action action) {
            if (action == null)
                ThrowHelper.ThrowArgumentNullException("action");

            Application.Current.Dispatcher.Invoke(action);
        }

        #endregion
        #region Run

        /// <summary>
        /// Asynchronously executes the specified <see cref="Action"/>.</summary>
        /// <param name="action">
        /// The <see cref="Action"/> to execute.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="action"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>Run</b> atomically increments <see cref="Count"/> before queuing the specified
        /// <paramref name="action"/> on the <see cref="ThreadPool"/>, and atomically decrements
        /// <see cref="Count"/> within the same <see cref="ThreadPool"/> thread just after the
        /// <paramref name="action"/> was executed.
        /// </para><note type="caution">
        /// <see cref="Count"/> correctly reflects the number of outstanding operations only if the
        /// specified <paramref name="action"/> does not initiate further asynchronous operations
        /// that are not guarded by <see cref="Count"/>. Specifically, the <paramref name="action"/>
        /// should use only <see cref="Invoke"/> and never <see cref="BeginInvoke"/> to marshal
        /// calls to the <see cref="Application"/> thread.</note></remarks>

        public static void Run(Action action) {
            if (action == null)
                ThrowHelper.ThrowArgumentNullException("action");

#pragma warning disable 0420
            Interlocked.Increment(ref _count);
            ThreadPool.QueueUserWorkItem(delegate {
                action();
                Interlocked.Decrement(ref _count);
            });
#pragma warning restore 0420
        }

        #endregion
    }
}
