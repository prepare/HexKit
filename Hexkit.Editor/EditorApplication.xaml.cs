using System;
using System.Text;
using System.Windows;
using System.Windows.Threading;

using Tektosyne.Windows;
using Hexkit.Global;
using Hexkit.Options;
using Hexkit.Scenario;

namespace Hexkit.Editor {

    /// <summary>
    /// Defines the Hexkit Editor application.</summary>

    public partial class EditorApplication: Application {
        #region EditorApplication()

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorApplication"/> class.</summary>
        /// <remarks><para>
        /// This constructor performs the following actions:
        /// </para><list type="bullet"><item>
        /// Call <see cref="ApplicationUtility.CheckSingleInstance"/> to make sure that the current
        /// user is not already running another instance of the <see cref="EditorApplication"/>.
        /// </item><item>
        /// Call <see cref="FilePaths.CheckUserFolders"/> to make sure that the current user has
        /// write permission in the directories that hold user-specific data.
        /// </item><item>
        /// Attach a handler to the <see cref="Application.DispatcherUnhandledException"/> event
        /// that calls <see cref="ApplicationUtility.OnUnhandledException"/>.
        /// </item><item>
        /// Create a global instance of the <see cref="ApplicationOptions"/> class and load the
        /// current user settings.</item></list></remarks>

        public EditorApplication() {

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

            // read user settings from Hexkit Editor options file
            ApplicationOptions options = ApplicationOptions.CreateInstance();

            // select user-defined display theme, if any
            if (options.View.Theme != DefaultTheme.System)
                DefaultThemeSetter.Select(options.View.Theme);
        }

        #endregion
        #region OnUnhandledException

        /// <summary>
        /// Handles the <see cref="Application.DispatcherUnhandledException"/> event for the <see
        /// cref="EditorApplication"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> sending the event.</param>
        /// <param name="args">
        /// A <see cref="DispatcherUnhandledExceptionEventArgs"/> object containing event data.
        /// </param>
        /// <remarks>
        /// <b>OnUnhandledException</b> handles all uncaught exceptions for the <see
        /// cref="EditorApplication"/> by gathering all relevant application data and then calling
        /// <see cref="ApplicationUtility.OnUnhandledException"/> for further processing.</remarks>

        private void OnUnhandledException(object sender,
            DispatcherUnhandledExceptionEventArgs args) {

            // store information about the exception
            StringBuilder info = new StringBuilder();
            info.Append(args.Exception);
            info.Append(Environment.NewLine);
            info.Append(Environment.NewLine);

            MasterSection scenario = MasterSection.Instance;
            Action<String> saveScenario = null;

            if (scenario == null) {
                info.Append("Scenario instance is null. ");
                info.Append(Environment.NewLine);
            } else {
                // delegate to save scenario data
                saveScenario = scenario.SaveAll;

                // collect various Hexkit Editor information
                foreach (ScenarioSection section in SectionUtility.AllSections) {
                    info.AppendFormat("Scenario.Section.{0}: {1} ",
                        section, scenario.SectionPaths.GetPath(section));
                    info.Append(Environment.NewLine);
                }
            }

            // invoke common exception handler
            ApplicationUtility.OnUnhandledException(info.ToString(), null, saveScenario, null);
        }

        #endregion
    }
}
