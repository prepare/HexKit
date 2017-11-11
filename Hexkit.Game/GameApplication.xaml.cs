using System;
using System.Text;
using System.Windows;
using System.Windows.Threading;

using Tektosyne;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Options;
using Hexkit.Scenario;

namespace Hexkit.Game {

    /// <summary>
    /// Defines the Hexkit Game application.</summary>

    public partial class GameApplication: Application {
        #region GameApplication()

        /// <summary>
        /// Initializes a new instance of the <see cref="GameApplication"/> class.</summary>
        /// <remarks><para>
        /// This constructor performs the following actions:
        /// </para><list type="bullet"><item>
        /// Call <see cref="ApplicationUtility.CheckSingleInstance"/> to make sure that the current
        /// user is not already running another instance of the <see cref="GameApplication"/>.
        /// </item><item>
        /// Call <see cref="FilePaths.CheckUserFolders"/> to make sure that the current user has
        /// write permission in the directories that hold user-specific data.
        /// </item><item>
        /// Attach a handler to the <see cref="Application.DispatcherUnhandledException"/> event
        /// that calls <see cref="ApplicationUtility.OnUnhandledException"/>.
        /// </item><item>
        /// Create a global instance of the <see cref="ApplicationOptions"/> class and load the
        /// current user settings.</item></list></remarks>

        public GameApplication() {

            // make sure this is the only instance
            if (!ApplicationUtility.CheckSingleInstance()) {
                Application.Current.Shutdown(-1);
                return;
            }

            // make sure we can create user files
            if (!FilePaths.CheckUserFolders()) {
                Application.Current.Shutdown(-2);
                return;
            }

            // hook up custom exception handler
            Application.Current.DispatcherUnhandledException += OnUnhandledException;

            // read user settings from Hexkit Game options file
            ApplicationOptions options = ApplicationOptions.CreateInstance();

            // select user-defined display theme, if any
            if (options.View.Theme != DefaultTheme.System)
                DefaultThemeSetter.Select(options.View.Theme);
        }

        #endregion
        #region OnUnhandledException

        /// <summary>
        /// Handles the <see cref="Application.DispatcherUnhandledException"/> event for the <see
        /// cref="GameApplication"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> sending the event.</param>
        /// <param name="args">
        /// A <see cref="DispatcherUnhandledExceptionEventArgs"/> object containing event data.
        /// </param>
        /// <remarks>
        /// <b>OnUnhandledException</b> handles all uncaught exceptions for the <see
        /// cref="GameApplication"/> by gathering all relevant application data and then calling
        /// <see cref="ApplicationUtility.OnUnhandledException"/> for further processing.</remarks>

        private void OnUnhandledException(object sender,
            DispatcherUnhandledExceptionEventArgs args) {

            // store information about the exception
            StringBuilder info = new StringBuilder();
            info.Append(args.Exception);
            info.Append(Environment.NewLine);
            info.Append(Environment.NewLine);

            // retrieve current scenario path, if any
            MasterSection scenario = MasterSection.Instance;
            string scenarioPath = (scenario == null ? "(no scenario)" :
                StringUtility.Validate(scenario.Path.RelativePath));

            // collect various game information
            info.AppendFormat("Session.State.{0} ", Session.State);
            info.Append(Environment.NewLine);
            info.AppendFormat("Scenario: \"{0}\"", scenarioPath);
            info.Append(Environment.NewLine);

            string rulesPath = null;
            Action<String> saveScenario = null, saveSession = null;

            // delegates to save scenario and session data
            if (scenario != null) {
                rulesPath = scenario.Rules.Path;
                saveScenario = scenario.SaveAll;
                if (Session.State != SessionState.Invalid)
                    saveSession = Session.Instance.SaveDirect;
            }

            // invoke common exception handler
            ApplicationUtility.OnUnhandledException(
                info.ToString(), rulesPath, saveScenario, saveSession);
        }

        #endregion
    }
}
