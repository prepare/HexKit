using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using Tektosyne;
using Tektosyne.IO;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.Editor {

    /// <summary>
    /// Provides the "Master" tab page for the Hexkit Editor application.</summary>
    /// <remarks>
    /// Please refer to the "Master Page" page of the "Editor Display" section in the application
    /// help file for details on this tab page.</remarks>

    public partial class MasterTabContent: UserControl, IEditorTabContent {
        #region MasterTabContent()

        /// <summary>
        /// Initializes a new instance of the <see cref="MasterTabContent"/> class.</summary>

        public MasterTabContent() {
            InitializeComponent();

            // labels to indicate DataChanged flags
            this._dataChangedLabels = new Label[] {
                null, // no entry for Master section
                ImagesChangedLabel,
                VariablesChangedLabel,
                EntitiesChangedLabel,
                FactionsChangedLabel,
                AreasChangedLabel
            };

            // text boxes to show section file paths
            this._sectionPathBoxes = new TextBox[] {
                null, // no entry for Master section
                ImagesPathBox,
                VariablesPathBox,
                EntitiesPathBox,
                FactionsPathBox,
                AreasPathBox
            };
        }

        #endregion
        #region Private Fields

        // property backers
        private SectionTabItem _sectionTab;

        // controls for subsection data
        private readonly Label[] _dataChangedLabels;
        private readonly TextBox[] _sectionPathBoxes;

        #endregion
        #region UpdateSubsection

        /// <summary>
        /// Updates the "Subsection Locations" display for the specified <see
        /// cref="ScenarioSection"/>, which must be a subsection.</summary>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the scenario subsection to update.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="section"/> equals <see cref="ScenarioSection.Master"/>.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="section"/> is not a valid <see cref="ScenarioSection"/> value.
        /// </exception>
        /// <remarks>
        /// <b>UpdatePath</b> updates the "Subsection Locations" line corresponding to the specified
        /// <paramref name="section"/> to reflect its <see cref="SectionTabItem.DataChanged"/> flag
        /// and its file path within the current <see cref="MasterSection"/>.</remarks>

        public void UpdateSubsection(ScenarioSection section) {
            switch (section) {

                case ScenarioSection.Master:
                    ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat("section",
                        section, Tektosyne.Strings.ArgumentEquals, ScenarioSection.Master);
                    break;

                case ScenarioSection.Images:
                case ScenarioSection.Variables:
                case ScenarioSection.Entities:
                case ScenarioSection.Factions:
                case ScenarioSection.Areas:

                    // show or hide change marker for section
                    int index = (int) section;
                    var tabPage = MainWindow.Instance.GetTabPage(section);
                    this._dataChangedLabels[index].Visibility =
                        (tabPage.DataChanged ? Visibility.Visible : Visibility.Hidden);

                    // show file path or "(inline)" for section
                    RootedPath path = MasterSection.Instance.SectionPaths.GetPath(section);
                    this._sectionPathBoxes[index].Text =
                        (path.IsEmpty ? Global.Strings.LabelInline : path.RelativePath);
                    break;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "section", (int) section, typeof(ScenarioSection));
                    break;
            }
        }

        #endregion
        #region Event Handlers
        #region OnRulesBrowse

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Browse" <see cref="Button"/>
        /// next to the "Rule Script" text box.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnRulesBrowse</b> displays an <see cref="FileDialogs.OpenRulesDialog"/> with the
        /// current contents of the "Rule Script" text box.
        /// </para><para>
        /// If the user selects a new path, <b>OnRulesBrowse</b> updates the "Rule Script" text box
        /// and the <see cref="RuleScript.Path"/> of the current <see cref="MasterSection.Rules"/>,
        /// and sets the <see cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void OnRulesBrowse(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // ask user to select an existing file
            RootedPath path = FileDialogs.OpenRulesDialog(RulesPathBox.Text);

            // set new path (triggers OnRulesChanged)
            if (!path.IsEmpty)
                RulesPathBox.Text = path.RelativePath;
        }

        #endregion
        #region OnRulesChanged

        /// <summary>
        /// Handles the <see cref="TextBoxBase.TextChanged"/> event for the "Rule Script" <see
        /// cref="TextBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="TextChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnRulesChanged</b> copies the current contents of the "Rule Script" text box to the
        /// <see cref="RuleScript.Path"/> of the current <see cref="MasterSection.Rules"/>, and sets
        /// the <see cref="SectionTabItem.DataChanged"/> flag.</remarks>

        private void OnRulesChanged(object sender, TextChangedEventArgs args) {
            args.Handled = true;

            // do nothing if rules path unchanged
            RuleScript rules = MasterSection.Instance.Rules;
            if (rules.Path == RulesPathBox.Text) return;

            // store new rules path in scenario section
            rules.Path = RulesPathBox.Text;

            // broadcast data changes
            SectionTab.DataChanged = true;
        }

        #endregion
        #region OnScenarioInfo

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Scenario Information" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnScenarioInfo</b> displays a <see cref="Dialog.ChangeInformation"/> dialog for the
        /// <see cref="MasterSection.Information"/> block of the current <see
        /// cref="MasterSection"/>, and sets the <see cref="SectionTabItem.DataChanged"/> flag if
        /// the user made any changes.</remarks>

        private void OnScenarioInfo(object sender, RoutedEventArgs args) {
            args.Handled = true;

            // retrieve current scenario information
            Information info = MasterSection.Instance.Information;

            // show dialog and let user make changes
            var dialog = new Dialog.ChangeInformation(info, Global.Strings.TitleChangeScenario);
            dialog.Owner = MainWindow.Instance;
            dialog.ShowDialog();

            // broadcast data changed by dialog
            if (dialog.DataChanged)
                SectionTab.DataChanged = true;
        }

        #endregion
        #region OnTitleChanged

        /// <summary>
        /// Handles the <see cref="TextBoxBase.TextChanged"/> event for the "Scenario Title" <see
        /// cref="TextBox"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="TextChangedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnTitleChanged</b> shows the contents of the "Scenario Title" text box in the main
        /// window caption of Hexkit Editor.
        /// </para><para>
        /// If the new title is different from the <see cref="MasterSection.Title"/> of the current
        /// <see cref="MasterSection"/>, <b>OnTitleChanged</b> also updates that property and and
        /// sets the <see cref="SectionTabItem.DataChanged"/> flag.</para></remarks>

        private void OnTitleChanged(object sender, TextChangedEventArgs args) {
            args.Handled = true;

            // show current title in application title bar
            string title = TitleBox.Text;
            MainWindow.Instance.Title = String.Format(
                ApplicationInfo.Culture, "{0} - {1}", ApplicationInfo.Title,
                StringUtility.Validate(title, Global.Strings.TitleScenarioUntitled));

            // broadcast data changes, if any
            if (title != MasterSection.Instance.Title) {
                MasterSection.Instance.Title = title;
                SectionTab.DataChanged = true;
            }
        }

        #endregion
        #endregion
        #region IEditorTabContent Members
        #region Section

        /// <summary>
        /// Gets the <see cref="ScenarioSection"/> managed by the tab page.</summary>
        /// <value>
        /// The constant value <see cref="ScenarioSection.Master"/>, indicating the Hexkit scenario
        /// section managed by the "Master" tab page.</value>

        public ScenarioSection Section {
            get { return ScenarioSection.Master; }
        }

        #endregion
        #region SectionTab

        /// <summary>
        /// Gets or sets the <see cref="SectionTabItem"/> for the tab page.</summary>
        /// <value>
        /// The <see cref="SectionTabItem"/> that contains the <see cref="MasterTabContent"/>
        /// control, i.e. the "Master" tab page of the Hexkit Editor application.</value>
        /// <exception cref="ArgumentNullException">
        /// The property is set to a null reference.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set more than once.</exception>

        public SectionTabItem SectionTab {
            [DebuggerStepThrough]
            get { return this._sectionTab; }
            set {
                if (value == null)
                    ThrowHelper.ThrowArgumentNullException("value");
                if (this._sectionTab != null)
                    ThrowHelper.ThrowInvalidOperationException(Tektosyne.Strings.PropertySetOnce);

                this._sectionTab = value;
            }
        }

        #endregion
        #region Initialize

        /// <summary>
        /// Initializes the section-specific controls of the tab page.</summary>
        /// <remarks>
        /// <b>Initialize</b> initializes all controls that are specific to the "Master" tab page,
        /// and also calls <see cref="MainWindow.UpdateTabPage"/> for all subsections to initialize
        /// their data from the current <see cref="MasterSection"/>. This is necessary because the
        /// Master section determines the contents (via file path or inlining) of all subsections.
        /// </remarks>

        public void Initialize() {

            // update subsections from file or scenario data
            foreach (ScenarioSection section in SectionUtility.Subsections)
                MainWindow.Instance.UpdateTabPage(section);

            // initialize controls on this page
            MasterSection scenario = MasterSection.Instance;
            TitleBox.Text = scenario.Title;
            RulesPathBox.Text = scenario.Rules.Path;
        }

        #endregion
        #endregion
    }
}
