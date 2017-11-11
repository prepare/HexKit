using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.World;
using Hexkit.World.Commands;
using Hexkit.World.Instructions;

namespace Hexkit.Game.Dialog {

    /// <summary>
    /// Shows a dialog with all faction and entity events in the specified <see cref="History"/>.
    /// </summary>
    /// <remarks>
    /// Please refer to the "Event History" page of the "Game Dialogs" section in the application
    /// help file for details on this dialog.</remarks>

    public partial class ShowEvents: Window {
        #region ShowEvents(History)

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowEvents"/> class with the specified <see
        /// cref="History"/>.</summary>
        /// <param name="history">
        /// The <see cref="History"/> whose <see cref="History.Factions"/> and <see
        /// cref="History.Entities"/> to show.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="history"/> is a null reference.</exception>

        public ShowEvents(History history) {
            if (history == null)
                ThrowHelper.ThrowArgumentNullException("history");

            this._history = history;
            InitializeComponent();

            #region Factions Tab

            // adjust column widths of Faction list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(FactionList, OnFactionWidthChanged);

            FactionList.ItemsSource = history.Factions.Keys;

            // select first faction, if any
            if (FactionList.Items.Count > 0)
                FactionList.SelectedIndex = 0;

            #endregion
            #region Entities Tab

            // adjust column widths of Entity list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(EntityList, OnEntityWidthChanged);

            // adjust column widths of Entity Event list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(EntityEventList, OnEntityEventWidthChanged);

            // use list buffer for natural sorting & virtualization
            var entities = new List<String>(history.Entities.Keys);
            entities.Sort(StringUtility.CompareOrdinal);
            EntityList.ItemsSource = entities;

            // select first entity, if any
            if (EntityList.Items.Count > 0)
                EntityList.SelectedIndex = 0;

            #endregion
        }

        #endregion
        #region Private Fields

        // construction arguments
        private readonly History _history;

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
        /// <b>HelpExecuted</b> opens the application help file on the help page for the current tab
        /// page of the <see cref="ShowEvents"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;

            // default to dialog help page
            string helpPage = "DlgShowEvents.html";

            // show help for specific tab page
            if (FactionsTab.IsSelected)
                helpPage = "DlgShowEventsFactions.html";
            else if (EntitiesTab.IsSelected)
                helpPage = "DlgShowEventsEntities.html";

            ApplicationUtility.ShowHelp(helpPage);
        }

        #endregion
        #region OnEntitySelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Entity" <see
        /// cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntitySelected</b> updates the "Entity Event" list view to reflect the selected
        /// <see cref="Entity"/>.</remarks>

        private void OnEntitySelected(object sender, SelectionChangedEventArgs args) {

            // clear entity event list view
            EntityEventList.ItemsSource = null;

            // retrieve selected entity, if any
            string id = EntityList.SelectedItem as string;
            if (String.IsNullOrEmpty(id)) return;

            // show event history for selected entity
            EntityEventList.ItemsSource = this._history.Entities[id].Events;
        }

        #endregion
        #region OnFactionSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Faction" <see
        /// cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFactionSelected</b> updates the "Faction Event" list view to reflect the selected
        /// <see cref="Faction"/>.</remarks>

        private void OnFactionSelected(object sender, SelectionChangedEventArgs args) {

            // clear faction event list view
            FactionEventList.ItemsSource = null;

            // retrieve selected faction, if any
            string id = FactionList.SelectedItem as string;
            if (String.IsNullOrEmpty(id)) return;

            // show event history for selected faction
            FactionEventList.ItemsSource = this._history.Factions[id].Events;
        }

        #endregion
        #region On...WidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Faction" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFactionWidthChanged</b> resizes the only column of the "Faction" list view to the
        /// current list view width.</remarks>

        private void OnFactionWidthChanged(object sender, EventArgs args) {

            double width = FactionList.ActualWidth - 28;
            if (width > 0) FactionColumn.Width = width;
        }

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Entity" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityWidthChanged</b> resizes the only column of the "Entity" list view to the
        /// current list view width.</remarks>

        private void OnEntityWidthChanged(object sender, EventArgs args) {

            double width = EntityList.ActualWidth - 28;
            if (width > 0) EntityColumn.Width = width;
        }

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Entity Event" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnEntityEventWidthChanged</b> resizes the "Value" column of the "Entity Event" list
        /// view to the available list view width.</remarks>

        private void OnEntityEventWidthChanged(object sender, EventArgs args) {

            double width = EntityEventList.ActualWidth -
                EntityTurnColumn.ActualWidth - EntityEventColumn.ActualWidth - 28;
            if (width > 0) EntityValueColumn.Width = width;
        }

        #endregion
    }
}
