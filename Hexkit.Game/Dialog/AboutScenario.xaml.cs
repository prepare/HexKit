using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.Game.Dialog {

    /// <summary>
    /// Shows a dialog with basic information on the current scenario.</summary>
    /// <remarks>
    /// Please refer to the "About Scenario" page of the "Game Dialogs" section in the application
    /// help file for details on this dialog.</remarks>

    public partial class AboutScenario: Window {
        #region AboutScenario()

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutScenario"/> class.</summary>

        public AboutScenario() {
            InitializeComponent();
            MasterSection scenario = MasterSection.Instance;
            if (scenario == null) return;

            // show scenario title
            ScenarioInfo.Text = scenario.Title;

            // show scenario information
            ScenarioAuthorInfo.Text = scenario.Information.Author;
            ScenarioVersionInfo.Text = scenario.Information.Version;
            ScenarioLegalInfo.Text = scenario.Information.Legal;

            // show graphics information
            GraphicsAuthorInfo.Text = scenario.Images.Information.Author;
            GraphicsVersionInfo.Text = scenario.Images.Information.Version;
            GraphicsLegalInfo.Text = scenario.Images.Information.Legal;

            // default to scenario details
            ShowDetails(false);
        }

        #endregion
        #region ShowDetails

        /// <summary>
        /// Shows the <see cref="Information.Paragraphs"/> collection for either the <see
        /// cref="MasterSection"/> or the <see cref="ImageSection"/>.</summary>
        /// <param name="isGraphics">
        /// <c>true</c> to show information on the current <see cref="ImageSection"/>; <c>false</c>
        /// to show information on the current <see cref="MasterSection"/>.</param>
        /// <remarks>
        /// <b>ShowDetails</b> also replaces the "Show Graphics Information" button with the "Show
        /// Scenario Information" button or vice versa, depending on the specified <paramref
        /// name="isGraphics"/> value.</remarks>

        private void ShowDetails(bool isGraphics) {
            if (isGraphics) {
                DetailsInfo.Text = String.Join(Environment.NewLine,
                    MasterSection.Instance.Images.Information.Paragraphs);

                ShowGraphicsButton.Visibility = Visibility.Collapsed;
                ShowScenarioButton.Visibility = Visibility.Visible;
            } else {
                DetailsInfo.Text = String.Join(Environment.NewLine,
                    MasterSection.Instance.Information.Paragraphs);

                ShowScenarioButton.Visibility = Visibility.Collapsed;
                ShowGraphicsButton.Visibility = Visibility.Visible;
            }
        }

        #endregion
        #region HelpExecuted

        /// <summary>
        /// Handles the <see cref="CommandBinding.Executed"/> event for the <see
        /// cref="ApplicationCommands.Help"/> command.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// An <see cref="ExecutedRoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>HelpExecuted</b> opens the application help file on the help page for the <see
        /// cref="AboutScenario"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgAboutScenario.html");
        }

        #endregion
        #region OnShowGraphics

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Show Graphics Information"
        /// <see cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnShowGraphics</b> shows informational text about the current <see
        /// cref="ImageSection"/>, and replaces the "Show Graphics Information" button with the
        /// "Show Scenario Information" button.</remarks>

        private void OnShowGraphics(object sender, RoutedEventArgs args) {
            args.Handled = true;
            ShowDetails(true);
        }

        #endregion
        #region OnShowScenario

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Show Scenario Information"
        /// <see cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnShowScenario</b> shows informational text about the current <see
        /// cref="MasterSection"/>, and replaces the "Show Scenario Information" button with the
        /// "Show Graphics Information" button.</remarks>

        private void OnShowScenario(object sender, RoutedEventArgs args) {
            args.Handled = true;
            ShowDetails(false);
        }

        #endregion
    }
}
