using System;
using System.ComponentModel;
using Microsoft.Win32;

using Tektosyne;
using Tektosyne.IO;
using Hexkit.Scenario;

namespace Hexkit.Global {

    /// <summary>
    /// Provides standard Windows file dialogs for the application.</summary>
    /// <remarks>
    /// All <b>FileDialogs</b> methods return <see cref="RootedPath"/> objects whose <see
    /// cref="RootedPath.RootFolder"/> equals either the current <see
    /// cref="FilePaths.CommonFolder"/> or the current <see cref="FilePaths.UserFolder"/>.
    /// </remarks>

    public static class FileDialogs {
        #region Private Methods
        #region OpenDialog

        /// <summary>
        /// Shows an <see cref="OpenFileDialog"/> allowing the user to enter or select a file to
        /// open.</summary>
        /// <param name="title">
        /// The title bar text for the <see cref="OpenFileDialog"/>.</param>
        /// <param name="filter">
        /// The file type filter for displayed files.</param>
        /// <param name="extension">
        /// The default extension for files entered by the user.</param>
        /// <param name="path">
        /// A <see cref="RootedPath"/> wrapping the absolute file path initially selected in the
        /// dialog.</param>
        /// <param name="directory">
        /// The directory initially shown in the dialog if <paramref name="path"/> is empty.</param>
        /// <param name="create">
        /// <c>true</c> if the user may enter nonexistent file names; or <c>false</c> if the user
        /// may only select existing files.</param>
        /// <returns>
        /// A <see cref="RootedPath"/> wrapping the absolute file path selected by the user, or an
        /// empty path if the user cancelled the dialog.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is a null reference.</exception>
        /// <remarks>
        /// The specified <paramref name="path"/> may be empty to indicate that no file should be
        /// initially selected. In this case, the dialog initially shows the specified <paramref
        /// name="directory"/>, which is otherwise ignored.</remarks>

        private static RootedPath OpenDialog(string title, string filter,
            string extension, RootedPath path, string directory, bool create) {

            if (path == null)
                ThrowHelper.ThrowArgumentNullException("path");

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = extension;
            dialog.Filter = filter;
            dialog.InitialDirectory = directory ?? "";
            dialog.RestoreDirectory = true;
            dialog.Title = title;
            dialog.ValidateNames = true;

            // pre-select specified file path, if any
            if (!path.IsEmpty) {
                dialog.InitialDirectory = path.DirectoryName;
                dialog.FileName = path.FileName;
            }

            // check existence or allow new files
            dialog.CheckFileExists = !create;
            dialog.CheckPathExists = !create;

            // return empty path if user cancels
            var owner = System.Windows.Application.Current.MainWindow;
            if (dialog.ShowDialog(owner) != true)
                return path.Clear();

            // return selected file path
            return path.Change(dialog.FileName);
        }

        #endregion
        #region SaveDialog

        /// <summary>
        /// Shows an <see cref="SaveFileDialog"/> allowing the user to enter or select a file to
        /// save.</summary>
        /// <param name="title">
        /// The title bar text for the <see cref="SaveFileDialog"/>.</param>
        /// <param name="filter">
        /// The file type filter for displayed files.</param>
        /// <param name="index">
        /// The index of the initially selected file type filter.</param>
        /// <param name="extension">
        /// The default extension for files entered by the user.</param>
        /// <param name="path">
        /// A <see cref="RootedPath"/> wrapping the absolute file path initially selected in the
        /// dialog.</param>
        /// <param name="directory">
        /// The directory initially shown in the dialog if <paramref name="path"/> is empty.</param>
        /// <returns>
        /// A <see cref="RootedPath"/> wrapping the absolute file path selected by the user, or an
        /// empty path if the user cancelled the dialog.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is a null reference.</exception>
        /// <remarks>
        /// The specified <paramref name="path"/> may be empty to indicate that no file should be
        /// initially selected. In this case, the dialog initially shows the specified <paramref
        /// name="directory"/>, which is otherwise ignored.</remarks>

        private static RootedPath SaveDialog(string title, string filter,
            int index, string extension, RootedPath path, string directory) {

            if (path == null)
                ThrowHelper.ThrowArgumentNullException("path");

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.DefaultExt = extension;
            dialog.Filter = filter;
            dialog.FilterIndex = index;
            dialog.InitialDirectory = directory ?? "";
            dialog.RestoreDirectory = true;
            dialog.Title = title;
            dialog.ValidateNames = true;

            // pre-select specified file path, if any
            if (!path.IsEmpty) {
                dialog.InitialDirectory = path.DirectoryName;
                dialog.FileName = path.FileName;
            }

            // allow creation of new files
            dialog.CheckFileExists = false;
            dialog.CheckPathExists = false;

            // return empty path if user cancels
            var owner = System.Windows.Application.Current.MainWindow;
            if (dialog.ShowDialog(owner) != true)
                return path.Clear();

            // return selected file path
            return path.Change(dialog.FileName);
        }

        #endregion
        #endregion
        #region OpenImageDialog

        /// <summary>
        /// Shows an <see cref="OpenFileDialog"/> allowing the user to select a file with a
        /// graphical tile set to open.</summary>
        /// <param name="file">
        /// The file initially selected in the dialog, relative to the current <see
        /// cref="FilePaths.CommonFolder"/>.</param>
        /// <returns>
        /// A <see cref="RootedPath"/> wrapping the absolute file path selected by the user, or an
        /// empty path if the user cancelled the dialog.</returns>
        /// <remarks><para>
        /// <b>OpenImageDialog</b> only allows the selection of existing files.
        /// </para><para>
        /// The specified <paramref name="file"/> may be a null reference or an empty string to
        /// indicate that no file should be initially selected. The dialog initially shows the
        /// default folder for the <see cref="ScenarioSection.Images"/> section, unless overriden by
        /// an absolute <paramref name="file"/> path.</para></remarks>

        public static RootedPath OpenImageDialog(string file) {

            string directory = FilePaths.GetSectionFolder(ScenarioSection.Images);
            return OpenDialog(Strings.TitleImageOpen, Strings.FilterImage, "png",
                FilePaths.CreateCommonPath(file), directory, false);
        }

        #endregion
        #region OpenRulesDialog

        /// <summary>
        /// Shows an <see cref="OpenFileDialog"/> allowing the user to select a rule script file to
        /// open.</summary>
        /// <param name="file">
        /// The file initially selected in the dialog, relative to the current <see
        /// cref="FilePaths.CommonFolder"/>.</param>
        /// <returns>
        /// A <see cref="RootedPath"/> wrapping the absolute file path selected by the user, or an
        /// empty path if the user cancelled the dialog.</returns>
        /// <remarks><para>
        /// <b>OpenRulesDialog</b> only allows the selection of existing files.
        /// </para><para>
        /// The specified <paramref name="file"/> may be a null reference or an empty string to
        /// indicate that no file should be initially selected. The dialog initially shows the <see
        /// cref="FilePaths.RulesFolder"/>, unless overriden by an absolute <paramref name="file"/>
        /// path.</para></remarks>

        public static RootedPath OpenRulesDialog(string file) {

            return OpenDialog(Strings.TitleRulesOpen, Strings.FilterRules, "cs",
                FilePaths.CreateCommonPath(file), FilePaths.RulesFolder, false);
        }

        #endregion
        #region OpenScenarioDialog

        /// <summary>
        /// Shows an <see cref="OpenFileDialog"/> allowing the user to enter or select a scenario
        /// file to open.</summary>
        /// <returns>
        /// A <see cref="RootedPath"/> wrapping the absolute file path selected by the user, or an
        /// empty path if the user cancelled the dialog.</returns>
        /// <remarks>
        /// <b>OpenScenarioDialog</b> only allows the selection of existing files. The dialog
        /// initially shows the <see cref="FilePaths.ScenarioFolder"/>.</remarks>

        public static RootedPath OpenScenarioDialog() {

            return OpenDialog(Strings.TitleScenarioOpen, Strings.FilterScenario, "xml",
                FilePaths.CreateCommonPath(), FilePaths.ScenarioFolder, false);
        }

        #endregion
        #region OpenSectionDialog

        /// <summary>
        /// Shows an <see cref="OpenFileDialog"/> allowing the user to enter or select a scenario
        /// section file to open.</summary>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the scenario section that the file
        /// represents.</param>
        /// <param name="file">
        /// The file initially selected in the dialog, relative to the current <see
        /// cref="FilePaths.CommonFolder"/>.</param>
        /// <param name="create">
        /// <c>true</c> if the user may enter nonexistent file names; or <c>false</c> if the user
        /// may only select an existing file.</param>
        /// <returns>
        /// A <see cref="RootedPath"/> wrapping the absolute file path selected by the user, or an
        /// empty path if the user cancelled the dialog.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="section"/> is not a valid <see cref="ScenarioSection"/> value.
        /// </exception>
        /// <remarks>
        /// The specified <paramref name="file"/> may be a null reference or an empty string to
        /// indicate that no file should be initially selected. The dialog initially shows the
        /// default folder for the specified <paramref name="section"/>, unless overriden by an
        /// absolute <paramref name="file"/> path.</remarks>

        public static RootedPath OpenSectionDialog(
            ScenarioSection section, string file, bool create) {

            string directory = FilePaths.GetSectionFolder(section);
            return OpenDialog(Strings.TitleSectionOpen, Strings.FilterSection,
                "xml", FilePaths.CreateCommonPath(file), directory, create);
        }

        #endregion
        #region OpenSessionDialog

        /// <summary>
        /// Shows an <see cref="OpenFileDialog"/> allowing the user to enter or select a session
        /// file to open.</summary>
        /// <returns>
        /// A <see cref="RootedPath"/> wrapping the absolute file path selected by the user, or an
        /// empty path if the user cancelled the dialog.</returns>
        /// <remarks>
        /// <b>OpenSessionDialog</b> only allows the selection of existing files. The dialog
        /// initially shows the <see cref="FilePaths.SessionFolder"/>.</remarks>

        public static RootedPath OpenSessionDialog() {

            return OpenDialog(Strings.TitleGameOpen, Strings.FilterGame, "xml.gz",
                FilePaths.CreateUserPath(), FilePaths.SessionFolder, false);
        }

        #endregion
        #region SaveSectionDialog

        /// <summary>
        /// Shows a <see cref="SaveFileDialog"/> allowing the user to enter or select a scenario
        /// section file to save to.</summary>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the scenario section that the file
        /// represents.</param>
        /// <param name="file">
        /// The file initially selected in the dialog, relative to the current <see
        /// cref="FilePaths.CommonFolder"/>.</param>
        /// <returns>
        /// A <see cref="RootedPath"/> wrapping the absolute file path selected by the user, or an
        /// empty path if the user cancelled the dialog.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="section"/> is not a valid <see cref="ScenarioSection"/> value.
        /// </exception>
        /// <remarks>
        /// The specified <paramref name="file"/> may be a null reference or an empty string to
        /// indicate that no file should be initially selected. The dialog initially shows the
        /// default folder for the specified <paramref name="section"/>, unless overriden by an
        /// absolute <paramref name="file"/> path.</remarks>

        public static RootedPath SaveSectionDialog(ScenarioSection section, string file) {

            string directory = FilePaths.GetSectionFolder(section);
            return SaveDialog(Strings.TitleSectionSave, Strings.FilterSection,
                1, "xml", FilePaths.CreateCommonPath(file), directory);
        }

        #endregion
        #region SaveSessionDialog

        /// <summary>
        /// Shows a <see cref="SaveFileDialog"/> allowing the user to enter or select a session file
        /// to save to.</summary>
        /// <param name="file">
        /// The file initially selected in the dialog, relative to the current <see
        /// cref="FilePaths.UserFolder"/>.</param>
        /// <returns>
        /// A <see cref="RootedPath"/> wrapping the absolute file path selected by the user, or an
        /// empty path if the user cancelled the dialog.</returns>
        /// <remarks>
        /// The specified <paramref name="file"/> may be a null reference or an empty string to
        /// indicate that no file should be initially selected. The dialog initially shows the <see
        /// cref="FilePaths.SessionFolder"/>, unless overriden by an absolute <paramref
        /// name="file"/> path.</remarks>

        public static RootedPath SaveSessionDialog(string file) {

            return SaveDialog(Strings.TitleGameSave, Strings.FilterGame, 2,
                "xml.gz", FilePaths.CreateUserPath(file), FilePaths.SessionFolder);
        }

        #endregion
    }
}
