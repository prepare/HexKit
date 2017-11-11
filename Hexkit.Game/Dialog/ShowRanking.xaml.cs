using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Graphics;
using Hexkit.Scenario;
using Hexkit.World;

namespace Hexkit.Game.Dialog {
    #region Type Aliases

    using ConditionList = KeyedList<ConditionParameter, Hexkit.Scenario.Condition>;

    // avoid confusion with System.Windows.Condition
    using Condition = Hexkit.Scenario.Condition;

    // name of comparison criterion, background brush, and tag or resource class
    using CompareListItem = Tuple<String, Brush, Object>;

    #endregion

    /// <summary>
    /// Shows a dialog that ranks all factions by their current and historical possessions.
    /// </summary>
    /// <remarks>
    /// Please refer to the "Faction Ranking" page of the "Game Dialogs" section in the application
    /// help file for details on this dialog.</remarks>

    public partial class ShowRanking: Window {
        #region ShowRanking(VariableClass, Boolean)

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowRanking"/> class with the specified
        /// <see cref="MapView"/>.</summary>
        /// <param name="mapView">
        /// The <see cref="MapView"/> whose <see cref="MapView.WorldState"/> contains the current
        /// and historical data to show.</param>
        /// <param name="showGraph">
        /// <c>true</c> to initially show the "Graphs" tab page; <c>false</c> to initially show the
        /// "Tables" tab page.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="mapView"/> is a null reference.</exception>

        public ShowRanking(MapView mapView, bool showGraph) {
            if (mapView == null)
                ThrowHelper.ThrowArgumentNullException("mapView");

            this._mapView = mapView;
            this._worldState = mapView.WorldState;

            InitializeComponent();
            HistoryGraphHost.Content = new HistoryGraphRenderer();

            // Tables page: adjust column width of Compare list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(CompareTableList, OnCompareTableWidthChanged);

            // adjust column width of Faction list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(FactionList, OnFactionWidthChanged);

            // Graphs page: adjust column width of Compare list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(CompareGraphList, OnCompareGraphWidthChanged);

            // add comparison items to list views
            CreateCompareTableRows();
            CreateCompareGraphRows();

            GraphsTab.IsSelected = showGraph;
        }

        #endregion
        #region Private Fields

        // construction arguments
        private readonly MapView _mapView;
        private readonly WorldState _worldState;

        #endregion
        #region Private Methods
        #region AddCompareRow

        /// <summary>
        /// Adds one <see cref="CompareListItem"/> with the specified column data to the specified
        /// <see cref="ListView"/>.</summary>
        /// <param name="listView">
        /// The <see cref="ListView"/> to receive the new item.</param>
        /// <param name="text">
        /// A <see cref="String"/> to display in the new item.</param>
        /// <param name="tag">
        /// An <see cref="Object"/> that serves as a general-purpose tag.</param>
        /// <remarks>
        /// <b>AddCompareRow</b> also assigns a faction <see cref="Brush"/> to the added item if the
        /// specified <paramref name="tag"/> object is a <see cref="FactionHistory"/>.</remarks>

        private static void AddCompareRow(ListView listView, string text, object tag) {

            Brush brush = null;
            var history = tag as FactionHistory;
            if (history != null)
                brush = MediaObjects.GetGradientBrush(history.FactionClass.Color);

            listView.Items.Add(new CompareListItem(text, brush, tag));
        }

        #endregion
        #region CreateCompareGraphRows

        /// <summary>
        /// Adds one <see cref="CompareListItem"/> for each possession to the "Compare" <see
        /// cref="ListView"/> on the <see cref="GraphsTab"/> page.</summary>
        /// <remarks>
        /// <b>CreateCompareGraphRows</b> adds three items for non-resource possessions whose tag
        /// objects are identifiers; and then all factions whose history has been recorded, with the
        /// associated <see cref="FactionHistory"/> objects.</remarks>

        private void CreateCompareGraphRows() {

            // add criteria related to owned units & sites
            AddCompareRow(CompareGraphList, Global.Strings.LabelUnits, "units");
            AddCompareRow(CompareGraphList, Global.Strings.LabelUnitStrength, "unit-strength");
            AddCompareRow(CompareGraphList, Global.Strings.LabelSites, "sites");

            CompareGraphList.AddSeparator();

            // add all factions whose history was recorded
            foreach (FactionHistory history in this._worldState.History.Factions.Values)
                AddCompareRow(CompareGraphList, history.FactionClass.Name, history);
        }

        #endregion
        #region CreateCompareTableRows

        /// <summary>
        /// Adds one <see cref="CompareListItem"/> for each possession to the "Compare" <see
        /// cref="ListView"/> on the <see cref="TablesTab"/> page.</summary>
        /// <remarks>
        /// <b>CreateCompareTableRows</b> adds five items for non-resource possessions whose tag
        /// objects are identifiers; and then any resources that are owned by at least one faction,
        /// with the associated <see cref="ResourceClass"/> objects.</remarks>

        private void CreateCompareTableRows() {

            // add criteria related to owned units
            AddCompareRow(CompareTableList, Global.Strings.LabelUnits, "units");
            AddCompareRow(CompareTableList, Global.Strings.LabelUnitStrength, "unit-strength");
            AddCompareRow(CompareTableList, Global.Strings.LabelUnitValues, "unit-values");

            // add criteria related to owned sites
            AddCompareRow(CompareTableList, Global.Strings.LabelSites, "sites");
            AddCompareRow(CompareTableList, Global.Strings.LabelSiteValues, "site-values");

            CompareTableList.AddSeparator();

            // add all resource owned by any factions
            foreach (var pair in MasterSection.Instance.Variables.Resources)
                foreach (Faction faction in this._worldState.Factions)
                    if (faction.Resources.ContainsId(pair.Key)) {
                        AddCompareRow(CompareTableList, pair.Value.Name, pair.Value);
                        break;
                    }
        }

        #endregion
        #region CreateFactionRows

        /// <summary>
        /// Adds one <see cref="RankingListItem"/> for each <see cref="Faction"/> in the game to the
        /// "Faction" <see cref="ListView"/> on the <see cref="TablesTab"/> page.</summary>
        /// <param name="resource">
        /// The <see cref="ResourceClass"/> by which factions are ranked, or a null reference when
        /// ranking by another criterion.</param>
        /// <param name="id">
        /// The <see cref="VariableClass.Id"/> string of the specified <paramref name="resource"/>,
        /// if valid; or a string identifying the actual ranking criterion otherwise.</param>
        /// <remarks>
        /// <b>CreateFactionRows</b> adds one item for each faction in the associated <see
        /// cref="WorldState"/> to the "Faction" <see cref="ListView"/>.</remarks>

        private void CreateFactionRows(ResourceClass resource, string id) {
            var items = new List<RankingListItem>(this._worldState.Factions.Count);

            // determine faction values for current comparison criterion
            foreach (Faction faction in this._worldState.Factions) {
                var item = new RankingListItem(faction);

                if (resource != null) {
                    // show stockpile of specified resource
                    Variable variable = faction.Resources[id];
                    if (variable != null) {
                        item.Value = variable.Value;
                        item.ValueText = variable.ToString();
                    }
                } else {
                    switch (id) {
                        case "sites":
                            // show number of owned sites
                            item.Value = faction.Sites.Count;
                            item.ValueText = item.Value.ToString("N0", ApplicationInfo.Culture);
                            break;

                        case "site-values":
                            // show sum of normalized site values
                            foreach (Site site in faction.Sites)
                                item.Value += site.Valuation;
                            item.ValueText = item.Value.ToString("N2", ApplicationInfo.Culture);
                            break;

                        case "units":
                            // show number of owned units
                            item.Value = faction.Units.Count;
                            item.ValueText = item.Value.ToString("N0", ApplicationInfo.Culture);
                            break;

                        case "unit-strength":
                            // show sum of current unit strength
                            item.Value = faction.UnitStrength;
                            item.ValueText = item.Value.ToString("N0", ApplicationInfo.Culture);
                            break;

                        case "unit-values":
                            // show sum of normalized unit values
                            foreach (Entity unit in faction.Units)
                                item.Value += unit.Valuation;
                            item.ValueText = item.Value.ToString("N2", ApplicationInfo.Culture);
                            break;

                        default:
                            ThrowHelper.ThrowArgumentExceptionWithFormat(
                                "id", Tektosyne.Strings.ArgumentSpecifiesInvalid, "criterion");
                            break;
                    }
                }

                items.Add(item);
            }

            // sort faction items by value of comparison criterion
            items.ShellSort(new Comparison<RankingListItem>((x, y) => (int) (y.Value - x.Value)));

            // prepend rank to each entry
            int rank = 1;
            foreach (RankingListItem item in items) {
                item.Rank = rank++;
                FactionList.Items.Add(item);
            }
        }

        #endregion
        #region GetCommonThreshold

        /// <summary>
        /// Returns a <see cref="String"/> that represents the common threshold value, if any, for
        /// all conditions based on the specified game parameter.</summary>
        /// <param name="victory">
        /// <c>true</c> to consider <see cref="FactionClass.VictoryConditions"/>; <c>false</c> to
        /// consider <see cref="FactionClass.DefeatConditions"/>.</param>
        /// <param name="parameter">
        /// A <see cref="ConditionParameter"/> value indicating which element of each <see
        /// cref="Condition"/> collection to consider.</param>
        /// <returns><para>
        /// An em dash (—) if no faction defines a <see cref="Condition"/> with the specified
        /// <paramref name="parameter"/>.
        /// </para><para>-or-</para><para>
        /// The <see cref="Condition.Threshold"/> value of each <see cref="Condition"/> with the
        /// specified <paramref name="parameter"/> if it is equal for all factions.
        /// </para><para>-or-</para><para>
        /// An asterisk "*" if neither of the above is true.</para></returns>

        private string GetCommonThreshold(bool victory, ConditionParameter parameter) {

            var factions = this._worldState.Factions;
            if (factions.Count == 0) return "—";

            // get presence and data of first faction's condition
            var conditions = GetFactionConditions(factions[0], victory);

            Condition firstCondition;
            bool firstPresent = conditions.TryGetValue(parameter, out firstCondition);

            // get other factions' conditions where present
            for (int i = 1; i < factions.Count; i++) {
                Condition condition;
                conditions = GetFactionConditions(factions[i], victory);
                bool present = conditions.TryGetValue(parameter, out condition);

                // check for different presence or condition
                if (present != firstPresent || (present && condition != firstCondition))
                    return "*";
            }

            // use common threshold or else common absence indicator
            return (firstPresent ?
                firstCondition.Threshold.ToString(ApplicationInfo.Culture) : "—");
        }

        #endregion
        #region GetFactionConditions

        /// <summary>
        /// Returns either the <see cref="FactionClass.VictoryConditions"/> or the <see
        /// cref="FactionClass.DefeatConditions"/> of the specified <see cref="Faction"/>.
        /// </summary>
        /// <param name="faction">
        /// The <see cref="Faction"/> to process.</param>
        /// <param name="victory">
        /// <c>true</c> to return the <see cref="FactionClass.VictoryConditions"/>; <c>false</c> to
        /// return the <see cref="FactionClass.DefeatConditions"/>.</param>
        /// <returns>
        /// The indicated <see cref="ConditionList"/> of the specified <paramref name="faction"/>.
        /// </returns>

        private static ConditionList GetFactionConditions(Faction faction, bool victory) {
            return (victory ?
                faction.FactionClass.VictoryConditions :
                faction.FactionClass.DefeatConditions);
        }

        #endregion
        #endregion
        #region Event Handlers
        #region HelpExecuted

        /// <summary>
        /// Handles the <see cref="CommandBinding.Executed"/> event for the <see
        /// cref="ApplicationCommands.Help"/> command.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// An <see cref="ExecutedRoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>HelpExecuted</b> opens the application help file on the help page for the current tab
        /// page of the <see cref="ShowRanking"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // default to dialog help page
            string helpPage = "DlgShowRanking.html";

            // show help for specific tab page
            if (TablesTab.IsSelected)
                helpPage = "DlgShowRankingTables.html";
            else if (GraphsTab.IsSelected)
                helpPage = "DlgShowRankingGraphs.html";

            ApplicationUtility.ShowHelp(helpPage);
        }

        #endregion
        #region OnAccept

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "OK" <see cref="Button"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnAccept</b> calls <see cref="Window.Close"/> on this dialog, and also <see
        /// cref="Window.Activate"/> on its <see cref="Window.Owner"/> if that <see cref="Window"/>
        /// is the <see cref="MainWindow"/>.
        /// </para><para>
        /// This event handler is necessary because the <see cref="Window.Hide"/> and <see
        /// cref="Window.Show"/> calls potentially performed by an owning <see cref="ShowFactions"/>
        /// dialog prevent the "OK" button from closing the dialog by setting the <see
        /// cref="Window.DialogResult"/> property, and also deactivate the <see cref="MainWindow"/>.
        /// </para></remarks>

        private void OnAccept(object sender, RoutedEventArgs args) {
            args.Handled = true;
            Close();

            if (Owner == MainWindow.Instance)
                Owner.Activate();
        }

        #endregion
        #region OnContentRendered

        /// <summary>
        /// Raises and handles the <see cref="Window.ContentRendered"/> event.</summary>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnContentRendered</b> raises the <see cref="Window.ContentRendered"/> event by
        /// calling the base class implementation of <see cref="Window.OnContentRendered"/>.
        /// </para><para>
        /// <b>OnContentRendered</b> then handles the <see cref="Window.ContentRendered"/> event by
        /// selecting the first item in the "Compare" list view on both tab pages.</para></remarks>

        protected override void OnContentRendered(EventArgs args) {
            base.OnContentRendered(args);

            // select first criterion on both tab pages
            CompareTableList.SelectedIndex = 0;
            CompareGraphList.SelectedIndex = 0;
        }

        #endregion
        #region OnTabSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the main <see
        /// cref="TabControl"/> of the <see cref="ShowRanking"/> dialog.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnTabSelected</b> selects the same comparison criterion on the currently selected tab
        /// page as on the previously selected tab page, if possible.</remarks>

        private void OnTabSelected(object sender, SelectionChangedEventArgs args) {

            // ignore unhandled events from nested controls
            if (args.OriginalSource != DialogTabControl) return;
            args.Handled = true;

            // determine current and previous Compare list view
            ListView currentList, previousList;
            if (DialogTabControl.SelectedItem == TablesTab) {
                currentList = CompareTableList;
                previousList = CompareGraphList;
            } else {
                currentList = CompareGraphList;
                previousList = CompareTableList;
            }

            // retrieve previously selected item, if any
            object previous = previousList.SelectedItem;
            if (!(previous is CompareListItem)) return;
            object tag = ((CompareListItem) previous).Item3;

            // select same item in current list view, if possible
            for (int i = 0; i < currentList.Items.Count; i++) {
                object current = currentList.Items[i];
                if (!(current is CompareListItem)) continue;

                if (((CompareListItem) current).Item3 == tag) {
                    currentList.SelectedIndex = i;
                    currentList.ScrollIntoView(current);
                    break;
                }
            }
        }

        #endregion
        #region OnCompareActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the "Compare" <see cref="ListView"/> on any tab page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnCompareActivate</b> displays a <see cref="ShowVariable"/> dialog containing
        /// information on the double-clicked item in the "Compare" list view for <see
        /// cref="ResourceClass"/> criteria, or a <see cref="ShowFactions"/> dialog for <see
        /// cref="FactionHistory"/> criteria.</remarks>

        private void OnCompareActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            ListView listView = (TablesTab.IsSelected ? CompareTableList : CompareGraphList);
            var listItem = ItemsControl.ContainerFromElement(listView, source) as ListViewItem;
            if (listItem == null || !(listItem.Content is CompareListItem)) return;
            var item = (CompareListItem) listItem.Content;

            // show info dialog for resources, if applicable
            var resource = item.Item3 as ResourceClass;
            if (resource != null) {
                var dialog = new ShowVariables(resource) { Owner = this };
                dialog.ShowDialog();
                return;
            }

            // show info dialog for factions, if applicable
            var history = item.Item3 as FactionHistory;
            if (history != null) {
                var dialog = new ShowFactions(this._mapView, history.FactionClass);
                dialog.Owner = this;
                dialog.ShowDialog();
            }
        }

        #endregion
        #region OnCompareGraphSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Compare" <see
        /// cref="ListView"/> on the <see cref="GraphsTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnCompareGraphSelected</b> updates the ranking graph reflect the selected ranking
        /// criterion.</remarks>

        private void OnCompareGraphSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;
            var renderer = (HistoryGraphRenderer) HistoryGraphHost.Content;

            // clear history graph if nothing selected
            int index = CompareGraphList.SelectedIndex;
            if (index < 0) {
                renderer.Clear();
                return;
            }

            // retrieve selected criterion
            object item = CompareGraphList.Items[index];
            if (item is CompareListItem)
                renderer.DrawGraph(((CompareListItem) item).Item3);
        }

        #endregion
        #region OnCompareTableSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Compare" <see
        /// cref="ListView"/> on the <see cref="TablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnCompareTableSelected</b> updates the "Defeat" and "Victory" text boxes and the
        /// "Faction" list view to reflect the selected ranking criterion.</remarks>

        private void OnCompareTableSelected(object sender, SelectionChangedEventArgs args) {
            args.Handled = true;

            // retrieve selected item, if any
            CompareListItem item = CompareTableList.SelectedItem as CompareListItem;

            // clear faction list
            FactionList.Items.Clear();

            // clear thresholds if selection cleared
            if (item == null) {
                DefeatInfo.Clear();
                VictoryInfo.Clear();
                return;
            }

            // retrieve selected criterion
            string id = item.Item3 as String;
            var resource = item.Item3 as ResourceClass;
            if (resource != null) id = resource.Id;

            // show faction ranking
            CreateFactionRows(resource, id);

            string defeatText = "—"; // em dash
            string victoryText = "—"; // em dash

            if (resource != null) {
                // show global thresholds for resource conditions
                if (resource.Defeat != Int32.MinValue)
                    defeatText = resource.Format(resource.Defeat, false);

                if (resource.Victory != Int32.MaxValue)
                    victoryText = resource.Format(resource.Victory, false);
            } else {
                ConditionParameter parameter = ConditionParameter.Turns;
                switch (id) {
                    case "sites": parameter = ConditionParameter.Sites; break;
                    case "units": parameter = ConditionParameter.Units; break;
                    case "unit-strength": parameter = ConditionParameter.UnitStrength; break;
                }

                // show common thresholds for specific conditions
                if (parameter != ConditionParameter.Turns) {
                    defeatText = GetCommonThreshold(false, parameter);
                    victoryText = GetCommonThreshold(true, parameter);
                }
            }

            // show or clear thresholds
            DefeatInfo.Text = defeatText;
            VictoryInfo.Text = victoryText;
        }

        #endregion
        #region OnFactionActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the "Faction" <see cref="ListView"/> on the <see
        /// cref="TablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFactionActivate</b> displays a <see cref="ShowFactions"/> dialog containing
        /// information on the selected item in the "Faction" list view, if any.</remarks>

        private void OnFactionActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var listItem = ItemsControl.ContainerFromElement(FactionList, source) as ListViewItem;
            if (listItem == null) return;

            // show info dialog for faction
            var item = (RankingListItem) listItem.Content;
            FactionClass faction = item.Faction.FactionClass;
            var dialog = new ShowFactions(this._mapView, faction) { Owner = this };
            dialog.ShowDialog();
        }

        #endregion
        #region On...WidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Compare" <see cref="ListView"/> on the <see cref="TablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnCompareTableWidthChanged</b> resizes the only column of the "Compare" list view to
        /// the current list view width.</remarks>

        private void OnCompareTableWidthChanged(object sender, EventArgs args) {

            double width = CompareTableList.ActualWidth - 28;
            if (width > 0) CompareTableColumn.Width = width;
        }

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Faction" <see cref="ListView"/> on the <see cref="TablesTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFactionWidthChanged</b> resizes the second column of the "Faction" list view to the
        /// available list view width.</remarks>

        private void OnFactionWidthChanged(object sender, EventArgs args) {

            double width = FactionList.ActualWidth -
                FactionRankColumn.ActualWidth - FactionValueColumn.ActualWidth - 28;

            if (width > 0) FactionColumn.Width = width;
        }

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Compare" <see cref="ListView"/> on the <see cref="GraphsTab"/> page.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnCompareGraphWidthChanged</b> resizes the only column of the "Compare" list view to
        /// the current list view width.</remarks>

        private void OnCompareGraphWidthChanged(object sender, EventArgs args) {

            double width = CompareGraphList.ActualWidth - 28;
            if (width > 0) CompareGraphColumn.Width = width;
        }

        #endregion
        #endregion
        #region Class HistoryGraphRenderer

        /// <summary>
        /// Shows the history graph on the <see cref="GraphsTab"/> page.</summary>

        private class HistoryGraphRenderer: FrameworkElement {
            #region Private Fields

            // drawing arguments
            private string _criterion;
            private FactionHistory _history;

            #endregion
            #region Private Methods
            #region DrawGraph

            /// <summary>
            /// Draws a set of line graphs connecting the specified point collections to the
            /// specified <see cref="DrawingContext"/>.</summary>
            /// <param name="context">
            /// The <see cref="DrawingContext"/> for the rendering.</param>
            /// <param name="bounds">
            /// The region within <paramref name="context"/>, in device-independent pixels, where to
            /// draw.</param>
            /// <param name="colors">
            /// An <see cref="Array"/> containing the <see cref="Color"/> for each graph.</param>
            /// <param name="points">
            /// An <see cref="Array"/> containing the <see cref="Point"/> collections that
            /// constitute each graph.</param>
            /// <param name="typeface">
            /// The <see cref="Typeface"/> to use for axis labels.</param>
            /// <exception cref="ArgumentException">
            /// <paramref name="colors"/> and <paramref name="points"/> contain a different number
            /// of elements.</exception>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="context"/>, <paramref name="colors"/>, <paramref name="points"/>, or
            /// <paramref name="typeface"/> is a null reference.</exception>
            /// <remarks>
            /// <b>DrawGraph</b> does nothing if the specified <paramref name="points"/> array is
            /// empty, or contains only empty <see cref="Point"/> collections.</remarks>

            private static void DrawGraph(DrawingContext context, Rect bounds,
                Color[] colors, Point[][] points, Typeface typeface) {

                if (context == null)
                    ThrowHelper.ThrowArgumentNullException("context");
                if (colors == null)
                    ThrowHelper.ThrowArgumentNullException("colors");
                if (points == null)
                    ThrowHelper.ThrowArgumentNullException("values");
                if (typeface == null)
                    ThrowHelper.ThrowArgumentNullException("typeface");

                if (colors.Length != points.Length)
                    ThrowHelper.ThrowArgumentExceptionWithFormat(
                        "colors", Tektosyne.Strings.ArgumentConflict, "points");

                // check for empty drawing region
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;

                Point minimum = new Point(Double.MaxValue, Double.MaxValue);
                Point maximum = new Point(Double.MinValue, Double.MinValue);

                // determine minimum and maximum coordinates
                foreach (Point[] line in points) {
                    if (line == null) continue;

                    foreach (Point point in line) {
                        if (minimum.X > point.X) minimum.X = point.X;
                        if (maximum.X < point.X) maximum.X = point.X;

                        if (minimum.Y > point.Y) minimum.Y = point.Y;
                        if (maximum.Y < point.Y) maximum.Y = point.Y;
                    }
                }

                // check for empty point collections
                if (minimum.X > maximum.X || minimum.Y > maximum.Y)
                    return;

                // ensure non-empty ranges along both axes
                if (minimum.X == maximum.X) { ++maximum.X; }
                if (minimum.Y == maximum.Y) { --minimum.Y; ++maximum.Y; }

                // determine horizontal and vertical scale
                double scaleX = bounds.Width / (maximum.X - minimum.X);
                double scaleY = bounds.Height / (maximum.Y - minimum.Y);

                // determine horizontal and vertical origin
                Point origin = new Point(
                    bounds.Left - scaleX * minimum.X,
                    bounds.Top + scaleY * maximum.Y);

                // create required pens
                Pen blackPen = new Pen(Brushes.Black, 1.0);
                Pen lightPen = new Pen(Brushes.LightGray, 1.0);

                // draw black lines of origin
                context.DrawLine(blackPen,
                    new Point(origin.X, bounds.Top), new Point(origin.X, bounds.Bottom));
                context.DrawLine(blackPen,
                    new Point(bounds.Left, origin.Y), new Point(bounds.Right, origin.Y));

                // calculate optimal axis divisions
                Point division = new Point(
                    GetAxisDivision(maximum.X - minimum.X),
                    GetAxisDivision(maximum.Y - minimum.Y));

                // draw vertical tick lines and labels
                for (double tick = (maximum.X / division.X) * division.X;
                    tick >= minimum.X; tick -= division.X) {

                    // draw division line
                    double x = origin.X + tick * scaleX;
                    context.DrawLine(lightPen, new Point(x, bounds.Top), new Point(x, bounds.Bottom));

                    // print label left of line
                    string tickText = tick.ToString("N0", ApplicationInfo.Culture);
                    var text = new FormattedText(tickText, ApplicationInfo.Culture,
                        FlowDirection.LeftToRight, typeface, 10, Brushes.Black);

                    context.DrawText(text, new Point(x - 12, bounds.Top + 2));
                }

                // draw horizontal tick lines and labels
                for (double tick = (maximum.Y / division.Y) * division.Y;
                    tick >= minimum.Y; tick -= division.Y) {

                    // draw division line
                    double y = origin.Y - Fortran.NInt(tick * scaleY);
                    context.DrawLine(lightPen, new Point(bounds.Left, y), new Point(bounds.Right, y));

                    // print label below line
                    string tickText = tick.ToString("N0", ApplicationInfo.Culture);
                    var text = new FormattedText(tickText, ApplicationInfo.Culture,
                        FlowDirection.LeftToRight, typeface, 10, Brushes.Black);

                    context.DrawText(text, new Point(bounds.Left + 4, y + 2));
                }

                // draw all point collections
                for (int i = 0; i < points.Length; i++) {
                    Point[] line = points[i];
                    if (line == null) continue;

                    // draw lines from one point to the next
                    Pen pen = new Pen(new SolidColorBrush(colors[i]), 2.0);
                    for (int j = 0; j < line.Length - 1; j++) {

                        Point p0 = new Point(origin.X + line[j].X * scaleX,
                            origin.Y - line[j].Y * scaleY);

                        Point p1 = new Point(origin.X + line[j + 1].X * scaleX,
                            origin.Y - line[j + 1].Y * scaleY);

                        context.DrawLine(pen, p0, p1);
                    }
                }
            }

            #endregion
            #region GetAxisDivision

            /// <summary>
            /// Calculates the optimal axis division for the specified range.</summary>
            /// <param name="range">
            /// The range of non-negative values to show along the axis.</param>
            /// <returns>
            /// The optimal axis division for the specified <paramref name="range"/>.</returns>

            private static int GetAxisDivision(double range) {

                double value = Math.Abs(range);
                int factor = 1;

                while (value > 100.0) {
                    value /= 10.0;
                    factor *= 10;
                }

                if (value >= 50.0) return 10 * factor;
                if (value >= 20.0) return 5 * factor;
                if (value >= 10.0) return 2 * factor;

                return factor;
            }

            #endregion
            #endregion
            #region Clear

            /// <summary>
            /// Clears the history graph.</summary>

            public void Clear() {
                this._history = null;
                this._criterion = null;

                InvalidateVisual();
            }

            #endregion
            #region DrawGraph

            /// <summary>
            /// Draws a new history graph based on the specified ranking criterion.</summary>
            /// <param name="criterion">
            /// A <see cref="FactionHistory"/> object whose recorded possessions to show, or a <see
            /// cref="String"/> identifying the ranking criterion for all factions.</param>
            /// <remarks><para>
            /// <b>DrawGraph</b> draws two or more graphs representing the recorded possessions in a
            /// specified <see cref="FactionHistory"/>, or another criterion throughout the entire
            /// <see cref="History"/> of the associated <see cref="WorldState"/>.
            /// </para><para>
            /// Except for the initial <see cref="FactionHistory.FactionEventType.Create"/> event,
            /// all event turn indices are displayed as one greater than recorded, so that the turn
            /// count axis is one-based rather than zero-based.</para></remarks>

            public void DrawGraph(object criterion) {

                this._history = criterion as FactionHistory;
                this._criterion = (this._history == null ?
                    criterion as string : this._history.Id);

                InvalidateVisual();
            }

            #endregion
            #region OnRender

            /// <summary>
            /// Renders the visual content of the <see cref="HistoryGraphRenderer"/>.</summary>
            /// <param name="context">
            /// The <see cref="DrawingContext"/> for the rendering.</param>
            /// <remarks>
            /// <b>OnRender</b> draws a history graph for the ranking criteria submitted to the last
            /// call to <see cref="DrawGraph"/>.</remarks>

            protected override void OnRender(DrawingContext context) {
                base.OnRender(context);
                if (this._criterion == null) return;

                Color[] colors;
                Point[][] points;
                ShowRanking dialog = (ShowRanking) Window.GetWindow(this);

                if (this._history == null) {
                    // display sites or units for all factions
                    History history = dialog._worldState.History;
                    colors = new Color[history.Factions.Count];
                    points = new Point[history.Factions.Count][];

                    int faction = 0;
                    foreach (FactionHistory cursor in history.Factions.Values) {
                        colors[faction] = cursor.FactionClass.Color;
                        points[faction] = new Point[cursor.Events.Length];

                        for (int i = 0; i < cursor.Events.Length; i++) {
                            FactionHistory.FactionEvent factionEvent = cursor.Events[i];

                            int value = 0;
                            switch (this._criterion) {
                                case "sites": value = factionEvent.Sites; break;
                                case "units": value = factionEvent.Units; break;
                                case "unit-strength": value = factionEvent.UnitStrength; break;
                            }

                            points[faction][i] = new Point(i, value);
                        }

                        ++faction;
                    }
                } else {
                    // display sites and units for specified faction
                    int count = this._history.Events.Length;
                    colors = new Color[] { Colors.Blue, Colors.Red };
                    points = new Point[][] { new Point[count], new Point[count] };

                    for (int i = 0; i < this._history.Events.Length; i++) {
                        FactionHistory.FactionEvent factionEvent = this._history.Events[i];
                        points[0][i] = new Point(i, factionEvent.Sites);
                        points[1][i] = new Point(i, factionEvent.Units);
                    }
                }

                /*
                 * To ensure that the right and bottom division lines
                 * are visible, we shrink the drawing region by (2,2)
                 * relative to the actually available drawing area.
                 */

                Rect bounds = new Rect(1, 1, ActualWidth - 2, ActualHeight - 2);
                context.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
                DrawGraph(context, bounds, colors, points, dialog.GetTypeface());
                context.Pop();
            }

            #endregion
        }

        #endregion
    }
}
