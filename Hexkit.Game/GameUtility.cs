using System;
using System.Collections.Generic;
using System.Windows;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.IO;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Players;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Game {

    /// <summary>
    /// Provides auxiliary methods for the Hexkit Game application.</summary>

    public static class GameUtility {
        #region DrawAttackArrows

        /// <summary>
        /// Draws attack arrows on the specified <see cref="MapView"/>, connecting the specified
        /// units with the specified target location.</summary>
        /// <param name="mapView">
        /// The <see cref="MapView"/> to draw on.</param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing the attacking <see cref="Unit"/> objects.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the attack.</param>
        /// <param name="checkRegion">
        /// <c>true</c> to only draw arrows if <paramref name="target"/> lies within the <see
        /// cref="MapView.SelectedRegion"/> of the specified <paramref name="mapView"/>;
        /// otherwise, <c>false</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="mapView"/> or <paramref name="units"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>DrawAttackArrows</b> adds one arrow from each unique <see cref="Entity.Site"/> in the
        /// specified <paramref name="units"/> collection to the specified <paramref name="target"/>
        /// site.
        /// </para><para>
        /// <b>DrawAttackArrows</b> always clears the <see cref="MapView.AttackArrows"/> collection
        /// of the specified <paramref name="mapView"/>, even if no elements are added for whatever
        /// reason.</para></remarks>

        public static void DrawAttackArrows(MapView mapView,
            IList<Entity> units, PointI target, bool checkRegion) {

            if (mapView == null)
                ThrowHelper.ThrowArgumentNullException("mapView");
            if (units == null)
                ThrowHelper.ThrowArgumentNullException("units");

            // check that we have any units
            mapView.AttackArrows.Clear();
            if (units.Count == 0) goto showArrows;

            // check that target is valid, if desired
            if (checkRegion && !mapView.InSelectedRegion(target))
                goto showArrows;

            // draw arrows from selected units to target
            for (int i = 0; i < units.Count; i++) {
                Entity unit = units[i];
                if (unit.Site == null) continue;

                // draw one arrow per unit site
                LineI line = new LineI(unit.Site.Location, target);
                if (line.Start != line.End && !mapView.AttackArrows.Contains(line))
                    mapView.AttackArrows.Add(line);
            }

        showArrows:
            mapView.ShowArrows();
        }

        #endregion
        #region DrawMoveArrows

        /// <summary>
        /// Draws movement arrows on the specified <see cref="MapView"/>, connecting the specified
        /// units with the specified target location.</summary>
        /// <param name="mapView">
        /// The <see cref="MapView"/> to draw on.</param>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing the moving <see cref="Unit"/> objects.</param>
        /// <param name="target">
        /// The coordinates of the <see cref="Site"/> that is the target of the move.</param>
        /// <param name="checkRegion">
        /// <c>true</c> to only draw arrows if <paramref name="target"/> lies within the <see
        /// cref="MapView.SelectedRegion"/> of the specified <paramref name="mapView"/>; otherwise,
        /// <c>false</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="mapView"/> or <paramref name="units"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>DrawMoveArrows</b> adds a sequence of arrows from the <see cref="Entity.Site"/> of
        /// the first element in the specified <paramref name="units"/> collection to the specified
        /// <paramref name="target"/> site, following the path returned by <see
        /// cref="Finder.FindMovePath"/>.
        /// </para><para>
        /// <b>DrawMoveArrows</b> always clears the <see cref="MapView.MoveArrows"/> collection of
        /// the specified <paramref name="mapView"/>, even if no elements are added for whatever
        /// reason.</para></remarks>

        public static void DrawMoveArrows(MapView mapView,
            IList<Entity> units, PointI target, bool checkRegion) {

            if (mapView == null)
                ThrowHelper.ThrowArgumentNullException("mapView");
            if (units == null)
                ThrowHelper.ThrowArgumentNullException("units");

            // check that we have any units
            mapView.MoveArrows.Clear();
            if (units.Count == 0) goto showArrows;

            // check that source & target are different
            Site sourceSite = units[0].Site;
            if (sourceSite.Location == target)
                goto showArrows;

            // check that target is valid, if desired
            if (checkRegion && !mapView.InSelectedRegion(target))
                goto showArrows;

            // find exact path we're going to take
            WorldState world = mapView.WorldState;
            Site targetSite = world.GetSite(target);
            var path = Finder.FindMovePath(world, units, sourceSite, targetSite, false);

            // draw arrows for steps along path, if any
            if (path != null) {
                IList<PointI> sites = path.Nodes;
                for (int i = 0; i < sites.Count - 1; i++) {
                    LineI line = new LineI(sites[i], sites[i + 1]);
                    mapView.MoveArrows.Add(line);
                }
            }

        showArrows:
            mapView.ShowArrows();
        }

        #endregion
        #region SaveAreas

        /// <summary>
        /// Shows a <see cref="FileDialogs.SaveSectionDialog"/> allowing the user to enter or select
        /// an <see cref="AreaSection"/> file to save the current <see cref="WorldState"/> to.
        /// </summary>
        /// <param name="file">
        /// The file initially selected in the dialog.</param>
        /// <remarks><para>
        /// The specified <paramref name="file"/> may be a null reference or an empty string to
        /// indicate that no file should be initially selected. Otherwise, any directory prefix it
        /// contains overrides the specified <paramref name="file"/>. Files without an absolute path
        /// are interpreted as relative to the <see cref="ScenarioSection.Areas"/> section folder.
        /// </para><para>
        /// <b>SaveAreas</b> attempts to create a new <see cref="AreaSection"/> object from the <see
        /// cref="WorldState"/> of the current session, using the latter's <see
        /// cref="WorldState.CreateAreaSection"/> method, and then invokes <see
        /// cref="ScenarioElement.Save"/> on the new <see cref="AreaSection"/> object. If either
        /// operation fails, an error message is shown but no exception is thrown.</para></remarks>

        public static void SaveAreas(string file) {

            // abort if no world state to save
            if (Session.Instance == null) return;

            // let user select file path for Areas section
            RootedPath path = FilePaths.GetSectionFile(ScenarioSection.Areas, file);
            path = FileDialogs.SaveSectionDialog(ScenarioSection.Areas, path.AbsolutePath);
            if (path.IsEmpty) return;

            try {
                // create Areas section from world state
                AreaSection areas = Session.Instance.WorldState.CreateAreaSection(0, 0, 0, 0);
                areas.Save(path.AbsolutePath);
            }
            catch (Exception e) {
                string message = String.Format(ApplicationInfo.Culture,
                    Global.Strings.DialogSectionSaveError, path.AbsolutePath);

                MessageDialog.Show(MainWindow.Instance, message,
                    Global.Strings.TitleSectionError, e, MessageBoxButton.OK, Images.Error);
            }
        }

        #endregion
        #region SaveScenario

        /// <summary>
        /// Saves the current scenario description to the specified path.</summary>
        /// <param name="path">
        /// The file path of the scenario description to write.</param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="path"/> is a null reference or an empty string.</exception>
        /// <remarks><para>
        /// <b>SaveScenario</b> attempts to write the data of the current <see
        /// cref="MasterSection"/> instance to the specified <paramref name="path"/>, using the <see
        /// cref="MasterSection.SaveAll"/> method. Success or failure are reported to the user, but
        /// exceptions raised by <b>SaveAll</b> are never propagated to the caller.
        /// </para><para>
        /// <b>SaveScenario</b> does nothing if the current <see cref="MasterSection"/> instance is
        /// a null reference.</para></remarks>

        public static void SaveScenario(string path) {
            if (String.IsNullOrEmpty(path))
                ThrowHelper.ThrowArgumentNullOrEmptyException("path");

            // silently quit if no scenario available
            if (MasterSection.Instance == null) return;

            try {
                // save scenario description
                MasterSection.Instance.SaveAll(path);

                // report success to user
                string message = String.Format(ApplicationInfo.Culture,
                    Global.Strings.DialogScenarioSave, path);

                MessageBox.Show(MainWindow.Instance, message, Global.Strings.TitleScenarioSave,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception e) {
                string message = String.Format(ApplicationInfo.Culture,
                    Global.Strings.DialogScenarioSaveError, path);

                MessageDialog.Show(MainWindow.Instance, message,
                    Global.Strings.TitleScenarioSave, e, MessageBoxButton.OK, Images.Error);
            }
        }

        #endregion
        #region ShowWinner

        /// <summary>
        /// Shows the specified winning faction and its controlling player.</summary>
        /// <param name="faction"><para>
        /// The <see cref="Faction"/> that won the game.
        /// </para><para>-or-</para><para>
        /// A null reference to indicate that all factions were defeated.</para></param>
        /// <exception cref="PropertyValueException">
        /// The current session <see cref="Session.State"/> is not <see
        /// cref="SessionState.Closed"/>.</exception>
        /// <remarks><para>
        /// <b>ShowWinner</b> displays a <see cref="Dialog.ShowEvent"/> dialog announcing total
        /// defeat if the specified <paramref name="faction"/> is a null reference, or the winning
        /// faction and its player otherwise.
        /// </para><para>
        /// <b>ShowWinner</b> then displays a <see cref="Dialog.ShowRanking"/> dialog, opened on the
        /// "Graphs" tab page.</para></remarks>

        public static void ShowWinner(Faction faction) {

            if (Session.State != SessionState.Closed)
                ThrowHelper.ThrowPropertyValueExceptionWithFormat("Session.State",
                    Session.State, Tektosyne.Strings.PropertyNotValue, SessionState.Closed);

            if (faction == null) {
                // all factions were defeated
                Dialog.ShowEvent.Show(MainWindow.Instance,
                    Global.Strings.DialogEventDefeatGlobal, Global.Strings.TitleEventDefeat);
            }
            else {
                Player player = PlayerManager.Instance.GetPlayer(faction);

                // show victorious faction and controlling player
                string message = String.Format(ApplicationInfo.Culture,
                    Global.Strings.DialogEventVictory, player.Name);

                string caption = String.Format(ApplicationInfo.Culture,
                    Global.Strings.TitleEventVictory, faction.Name);

                Dialog.ShowEvent.Show(MainWindow.Instance, message, caption);
            }

            // show history graph for entire game
            var dialog = new Dialog.ShowRanking(Session.MapView, true);
            dialog.Owner = MainWindow.Instance;
            dialog.ShowDialog();
        }

        #endregion
    }
}
