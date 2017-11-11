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
    /// Shows a dialog with all commands in the specified <see cref="History"/>.</summary>
    /// <remarks>
    /// Please refer to the "Command History" page of the "Game Dialogs" section in the application
    /// help file for details on this dialog.</remarks>

    public partial class ShowCommands: Window {
        #region ShowCommands(History)

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowCommands"/> class with the specified
        /// <see cref="History"/>.</summary>
        /// <param name="history">
        /// The <see cref="History"/> whose <see cref="History.Commands"/> to show.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="history"/> is a null reference.</exception>

        public ShowCommands(History history) {
            if (history == null)
                ThrowHelper.ThrowArgumentNullException("history");

            InitializeComponent();

            // adjust column widths of Command list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(CommandList, OnCommandWidthChanged);

            // add history commands to list view
            var items = new List<CommandListItem>(history.Commands.Count);
            foreach (Command command in history.Commands) {
                CommandListItem item = new CommandListItem();

                // show one-based turn index
                item.Turn = (command.Turn + 1).ToString(ApplicationInfo.Culture);

                // show faction name or identifier
                item.Faction = command.Faction.Name;

                // show command text if present
                item.CommandText = StringUtility.Validate(command, "—"); // em dash

                // count command's message events
                int messages = 0;
                foreach (Instruction instruction in command.Program)
                    if (instruction is MessageInstruction)
                        ++messages;

                // show message event count if positive
                item.Events = "—"; // em dash
                if (messages > 0) item.Events = messages.ToString(ApplicationInfo.Culture);

                // store command reference
                item.Command = command;
                items.Add(item);
            }

            // use ItemsSource to enable virtualization
            CommandList.ItemsSource = items;

            // select last command, if any
            int count = CommandList.Items.Count;
            CommandList.SelectAndShow(count - 1);
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
        /// cref="ShowCommands"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgShowCommands.html");
        }

        #endregion
        #region OnCommandSelected

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the "Command" <see
        /// cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="SelectionChangedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnCommandSelected</b> updates the text box to reflect the selected command.</remarks>

        private void OnCommandSelected(object sender, SelectionChangedEventArgs args) {

            // clear command text box
            CommandInfo.Text = "";

            // retrieve selected command, if any
            CommandListItem item = CommandList.SelectedItem as CommandListItem;
            if (item == null) return;

            // show command text
            CommandInfo.Text = item.Command.ToString();
            CommandInfo.AppendText(Environment.NewLine);

            // show message events but suppress dialogs
            foreach (Instruction instruction in item.Command.Program) {
                MessageInstruction message = instruction as MessageInstruction;
                if (message != null)
                    SessionExecutor.ShowMessageEvent(message, CommandInfo, false);
            }
        }

        #endregion
        #region OnCommandWidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Command" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnCommandWidthChanged</b> resizes the "Faction" and "Command" columns of the
        /// "Command" list view so that each occupies one third and two thirds, respectively, of the
        /// available list view width.</remarks>

        private void OnCommandWidthChanged(object sender, EventArgs args) {

            double width = (CommandList.ActualWidth -
                TurnColumn.ActualWidth - EventsColumn.ActualWidth - 28);

            if (width > 0) {
                FactionColumn.Width = width / 3.0;
                CommandColumn.Width = (2.0 * width) / 3.0;
            }
        }

        #endregion
        #region Class CommandListItem

        /// <summary>
        /// Provides the data for a <see cref="ListViewItem"/> in the "Command" <see
        /// cref="ListView"/>.</summary>

        private class CommandListItem {
            public Command Command { get; set; }
            public string CommandText { get; set; }
            public string Turn { get; set; }
            public string Faction { get; set; }
            public string Events { get; set; }
        }

        #endregion
    }
}
