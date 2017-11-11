using System;
using Hexkit.Scenario;

namespace Hexkit.Editor {

    /// <summary>
    /// Provides common functionality for section-specific content on any tab page of the Hexkit
    /// Editor application.</summary>

    public interface IEditorTabContent {
        #region Section

        /// <summary>
        /// Gets the <see cref="ScenarioSection"/> managed by the tab page.</summary>
        /// <value>
        /// A <see cref="ScenarioSection"/> value indicating the Hexkit scenario section managed by
        /// the tab page.</value>

        ScenarioSection Section { get; }

        #endregion
        #region SectionTab

        /// <summary>
        /// Gets or sets the <see cref="SectionTabItem"/> for the tab page.</summary>
        /// <value>
        /// The <see cref="SectionTabItem"/> that contains the <see cref="IEditorTabContent"/>
        /// control, i.e. one of the tab pages of the Hexkit Editor application.</value>
        /// <exception cref="ArgumentNullException">
        /// The property is set to a null reference.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set more than once.</exception>

        SectionTabItem SectionTab { get; set;  }

        #endregion
        #region Initialize

        /// <summary>
        /// Initializes the section-specific controls of the tab page.</summary>
        /// <remarks>
        /// <b>Initialize</b> is called by the <see cref="SectionTabItem.Initialize"/> method of the
        /// hosting <see cref="SectionTab"/> before any other actions are performed.</remarks>

        void Initialize();

        #endregion
    }
}
