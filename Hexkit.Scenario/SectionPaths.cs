using System;
using System.ComponentModel;

using Tektosyne;
using Tektosyne.IO;
using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Manages file paths for the sections of a Hexkit scenario.</summary>

    public class SectionPaths {
        #region SectionPaths()

        /// <summary>
        /// Initializes a new instance of the <see cref="SectionPaths"/> class.</summary>

        public SectionPaths() {
            Areas = FilePaths.CreateCommonPath();
            Entities = FilePaths.CreateCommonPath();
            Factions = FilePaths.CreateCommonPath();
            Images = FilePaths.CreateCommonPath();
            Master = FilePaths.CreateCommonPath();
            Variables = FilePaths.CreateCommonPath();
        }

        #endregion
        #region Areas

        /// <summary>
        /// Gets the file path of the <see cref="AreaSection"/>.</summary>
        /// <value>
        /// A <see cref="RootedPath"/> wrapping the absolute path to the <see cref="AreaSection"/>
        /// file. The default is an empty path.</value>
        /// <remarks>
        /// <b>Areas</b> never returns a null reference, but it may return an empty path to indicate
        /// that the <see cref="AreaSection"/> is undefined or embedded in the <see cref="Master"/>
        /// file.</remarks>

        public RootedPath Areas { get; private set; }

        #endregion
        #region Entities

        /// <summary>
        /// Gets the file path of the <see cref="EntitySection"/>.</summary>
        /// <value>
        /// A <see cref="RootedPath"/> wrapping the absolute path to the <see cref="EntitySection"/>
        /// file. The default is an empty path.</value>
        /// <remarks>
        /// <b>Entities</b> never returns a null reference, but it may return an empty path to
        /// indicate that the <see cref="EntitySection"/> is undefined or embedded in the <see
        /// cref="Master"/> file.</remarks>

        public RootedPath Entities { get; private set; }

        #endregion
        #region Factions

        /// <summary>
        /// Gets the file path of the <see cref="FactionSection"/>.</summary>
        /// <value>
        /// A <see cref="RootedPath"/> wrapping the absolute path to the <see
        /// cref="FactionSection"/> file. The default is an empty path.</value>
        /// <remarks>
        /// <b>Factions</b> never returns a null reference, but it may return an empty path to
        /// indicate that the <see cref="FactionSection"/> is undefined or embedded in the <see
        /// cref="Master"/> file.</remarks>

        public RootedPath Factions { get; private set; }

        #endregion
        #region Images

        /// <summary>
        /// Gets the file path of the <see cref="ImageSection"/>.</summary>
        /// <value>
        /// A <see cref="RootedPath"/> wrapping the absolute path to the <see cref="ImageSection"/>
        /// file. The default is an empty path.</value>
        /// <remarks>
        /// <b>Images</b> never returns a null reference, but it may return an empty path to
        /// indicate that the <see cref="ImageSection"/> is undefined or embedded in the <see
        /// cref="Master"/> file.</remarks>

        public RootedPath Images { get; private set; }

        #endregion
        #region Master

        /// <summary>
        /// Gets the file path of the <see cref="MasterSection"/>.</summary>
        /// <value>
        /// A <see cref="RootedPath"/> wrapping the absolute path to the <see cref="MasterSection"/>
        /// file (not the temporary <see cref="ScenarioFileType.Start"/> file). The default is an
        /// empty path.</value>
        /// <remarks>
        /// <b>Master</b> never returns a null reference, but it may return an empty path to
        /// indicate that the <see cref="MasterSection"/> is undefined.</remarks>

        public RootedPath Master { get; private set; }

        #endregion
        #region Variables

        /// <summary>
        /// Gets the file path of the <see cref="VariableSection"/>.</summary>
        /// <value>
        /// A <see cref="RootedPath"/> wrapping the absolute path to the <see
        /// cref="VariableSection"/> file. The default is an empty path.</value>
        /// <remarks>
        /// <b>Variables</b> never returns a null reference, but it may return an empty path to
        /// indicate that the <see cref="VariableSection"/> is undefined or embedded in the <see
        /// cref="Master"/> file.</remarks>

        public RootedPath Variables { get; private set; }

        #endregion
        #region GetPath

        /// <summary>
        /// Gets the file path of the specified <see cref="ScenarioSection"/>.</summary>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the scenario section whose file path to
        /// return.</param>
        /// <returns>
        /// The <see cref="RootedPath"/> corresponding to the specified <paramref name="section"/>.
        /// </returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="section"/> is not a valid <see cref="ScenarioSection"/> value.
        /// </exception>
        /// <remarks>
        /// <b>GetPath</b> returns the value of the <see cref="SectionPaths"/> property
        /// corresponding to the specified <paramref name="section"/>.</remarks>

        public RootedPath GetPath(ScenarioSection section) {
            switch (section) {

                case ScenarioSection.Areas:     return Areas;
                case ScenarioSection.Entities:  return Entities;
                case ScenarioSection.Factions:  return Factions;
                case ScenarioSection.Images:    return Images;
                case ScenarioSection.Master:    return Master;
                case ScenarioSection.Variables: return Variables;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "section", (int) section, typeof(ScenarioSection));
                    return null;
            }
        }

        #endregion
        #region SetPath

        /// <summary>
        /// Sets the file path for the specified <see cref="ScenarioSection"/>.</summary>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the scenario section whose file path to
        /// set.</param>
        /// <param name="path"><para>
        /// The new file path for the specified <paramref name="section"/>.
        /// </para><para>-or-</para><para>
        /// A null reference or an empty string to indicate that the <paramref name="section"/> is
        /// undefined or embedded in the <see cref="Master"/> file.</para></param>
        /// <exception cref="InvalidOperationException">
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</exception>
        /// <remarks>
        /// <b>SetPath</b> changes the <see cref="SectionPaths"/> property corresponding to the
        /// specified <paramref name="section"/> to the specified <paramref name="path"/>.</remarks>

        public void SetPath(ScenarioSection section, string path) {
            ApplicationInfo.CheckEditor();
            SetPathUnchecked(section, path);
        }

        #endregion
        #region SetPathUnchecked

        /// <summary>
        /// Sets the file path for the specified <see cref="ScenarioSection"/>.</summary>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the scenario section whose file path to
        /// set.</param>
        /// <param name="path"><para>
        /// The new file path for the specified <paramref name="section"/>.
        /// </para><para>-or-</para><para>
        /// A null reference or an empty string to indicate that the <paramref name="section"/> is
        /// undefined or embedded in the <see cref="Master"/> file.</para></param>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="section"/> is not a valid <see cref="ScenarioSection"/> value.
        /// </exception>
        /// <remarks><para>
        /// <b>SetPathUnchecked</b> changes the <see cref="SectionPaths"/> property corresponding to
        /// the specified <paramref name="section"/> to the specified <paramref name="path"/>.
        /// </para><para>
        /// <b>SetPathUnchecked</b> is equivalent to <see cref="SetPath"/> but does not check the
        /// value of <see cref="ApplicationInfo.IsEditor"/>.</para></remarks>

        internal void SetPathUnchecked(ScenarioSection section, string path) {
            switch (section) {

                case ScenarioSection.Areas:
                    Areas = Areas.Change(path);
                    break;

                case ScenarioSection.Entities:
                    Entities = Entities.Change(path);
                    break;

                case ScenarioSection.Factions:
                    Factions = Factions.Change(path);
                    break;

                case ScenarioSection.Images:
                    Images = Images.Change(path);
                    break;

                case ScenarioSection.Master:
                    Master = Master.Change(path);
                    break;

                case ScenarioSection.Variables:
                    Variables = Variables.Change(path);
                    break;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "section", (int) section, typeof(ScenarioSection));
                    break;
            }
        }

        #endregion
    }
}
