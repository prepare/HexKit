using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

using Tektosyne;
using Tektosyne.Windows;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.Editor.Dialog {
    #region Type Aliases

    // faction ID, color and brush created from color
    using FactionListItem = Tuple<String, Color, SolidColorBrush>;

    #endregion

    /// <summary>
    /// Shows a dialog allowing the user to change the color codes for all factions.</summary>
    /// <remarks>
    /// Please refer to the "Change Faction Colors" page of the "Editor Dialogs" section in the
    /// application help file for details on this dialog.</remarks>

    public partial class ChangeColors: Window {
        #region ChangeColors()

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeColors"/> class.</summary>
        /// <remarks><para>
        /// <b>ChangeColors</b> may change the <see cref="FactionClass.Color"/> property of any <see
        /// cref="FactionClass"/> defined by the current <see cref="FactionSection"/>.
        /// </para><para>
        /// The value of the <see cref="DataChanged"/> property indicates whether any changes were
        /// made.</para></remarks>

        public ChangeColors() {
            InitializeComponent();

            // add factions to list view
            FactionSection factions = MasterSection.Instance.Factions;
            foreach (FactionClass faction in factions.Collection.Values) {

                // store faction ID with faction color
                FactionListItem item = new FactionListItem(
                    faction.Id, faction.Color, new SolidColorBrush(faction.Color));

                FactionList.Items.Add(item);
            }

            // adjust column widths of Faction list view
            DependencyPropertyDescriptor.FromProperty(
                ListView.ActualWidthProperty, typeof(ListView))
                .AddValueChanged(FactionList, OnFactionWidthChanged);

            // select first faction if present
            if (FactionList.Items.Count > 0)
                FactionList.SelectedIndex = 0;
            else {
                // disable buttons otherwise
                ChangeButton.IsEnabled = false;
                ResetButton.IsEnabled = false;
            }
        }

        #endregion
        #region DataChanged

        /// <summary>
        /// Gets a value indicating whether any data was changed in the dialog.</summary>
        /// <value>
        /// <c>true</c> if the current <see cref="FactionSection"/> has been modified; otherwise,
        /// <c>false</c>.</value>
        /// <remarks>
        /// <b>DataChanged</b> returns <c>false</c> if no detectable changes were made. However, the
        /// original data may have been overwritten with a copy that is not detectably different,
        /// namely if the user clicked <b>OK</b> without making any changes.</remarks>

        public bool DataChanged { get; private set; }

        #endregion
        #region Private Methods
        #region ChangeColor

        /// <summary>
        /// Allows the user to change the <see cref="FactionClass"/> color associated with the item
        /// at the specified index in the "Faction" <see cref="ListView"/>.</summary>
        /// <param name="index">
        /// The index of the <see cref="FactionListItem"/> to change.</param>
        /// <remarks>
        /// <b>ChangeColor</b> displays a <see cref="CustomColorDialog"/>, allowing the user to
        /// change the <see cref="FactionClass"/> color associated with the specified <paramref
        /// name="index"/>, and sets the <see cref="DataChanged"/> flag if the user made any
        /// changes.</remarks>

        private void ChangeColor(int index) {
            if (index < 0) return;
            FactionListItem item = (FactionListItem) FactionList.Items[index];

            // retrieve item color and let user change it
            Color color = item.Item2;
            bool result = CustomColorDialog.Show(this, ref color);

            // update item and brodcast changes if confirmed
            if (result && color != item.Item2) {
                FactionList.Items[index] = new FactionListItem(
                    item.Item1, color, new SolidColorBrush(color));

                DataChanged = true;
            }
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
        /// <b>HelpExecuted</b> opens the application help file on the help page for the <see
        /// cref="ChangeColors"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgChangeColors.html");
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
        /// <remarks>
        /// <b>OnAccept</b> sets the <see cref="Window.DialogResult"/> property to <c>true</c>. This
        /// also triggers the <see cref="Window.Closing"/> event.</remarks>

        private void OnAccept(object sender, RoutedEventArgs args) {
            args.Handled = true;
            DialogResult = true;
        }

        #endregion
        #region OnClosing

        /// <summary>
        /// Raises and handles the <see cref="Window.Closing"/> event.</summary>
        /// <param name="args">
        /// A <see cref="CancelEventArgs"/> object containing event data.</param>
        /// <remarks><para>
        /// <b>OnClosing</b> raises the <see cref="Window.Closing"/> event by calling the base class
        /// implementation of <see cref="Window.OnClosing"/> with the specified <paramref
        /// name="args"/>.
        /// </para><para>
        /// If the event was not requested to <see cref="CancelEventArgs.Cancel"/>, <b>OnClosing</b>
        /// handles the <see cref="Window.Closing"/> event by clearing the <see cref="DataChanged"/>
        /// flag if the <see cref="Window.DialogResult"/> is not <c>true</c>, indicating that the
        /// user cancelled the dialog and wants to discard all changes.
        /// </para><para>
        /// Otherwise, <b>OnClosing</b> reads the control contents of this dialog into the current
        /// <see cref="FactionSection"/>.</para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // user cancelled dialog, ignore changes
            if (DialogResult != true) {
                DataChanged = false;
                return;
            }

            // store new faction colors
            FactionSection factions = MasterSection.Instance.Factions;
            foreach (FactionListItem item in FactionList.Items) {
                FactionClass faction = factions.Collection[item.Item1];
                faction.Color = item.Item2;
            }
        }

        #endregion
        #region OnColorChange

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Change Color" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnColorChange</b> calls <see cref="ChangeColor"/> with the first selected item in the
        /// "Faction" list view.</remarks>

        private void OnColorChange(object sender, RoutedEventArgs args) {
            args.Handled = true;
            ChangeColor(FactionList.SelectedIndex);
        }

        #endregion
        #region OnColorsReset

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Reset Colors" <see
        /// cref="Button"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnColorsReset</b> resets the colors of all items in the "Faction" list view to the
        /// corresponding <see cref="FactionClass.DefaultColors"/>, and sets the <see
        /// cref="DataChanged"/> flag if any changes were made.</remarks>

        private void OnColorsReset(object sender, RoutedEventArgs args) {
            args.Handled = true;

            int count = FactionClass.DefaultColors.Length;
            for (int i = 0; i < FactionList.Items.Count; i++) {

                // retrieve next default color in sequence
                Color color = FactionClass.DefaultColors[i % count];

                // skip factions with default color
                FactionListItem item = (FactionListItem) FactionList.Items[i];
                if (color == item.Item2) continue;

                // broadcast data changes
                FactionList.Items[i] = new FactionListItem(
                    item.Item1, color, new SolidColorBrush(color));

                DataChanged = true;
            }
        }

        #endregion
        #region OnFactionActivate

        /// <summary>
        /// Handles the <see cref="Control.MouseDoubleClick"/> event for a <see
        /// cref="ListViewItem"/> of the "Faction" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="MouseButtonEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFactionActivate</b> calls <see cref="ChangeColor"/> with the double-clicked item in
        /// the "Faction" list view.</remarks>

        private void OnFactionActivate(object sender, MouseButtonEventArgs args) {
            args.Handled = true;

            // retrieve double-clicked item, if any
            var source = args.OriginalSource as DependencyObject;
            var item = ItemsControl.ContainerFromElement(FactionList, source) as ListViewItem;
            if (item != null) ChangeColor(FactionList.Items.IndexOf(item.Content));
        }

        #endregion
        #region OnFactionWidthChanged

        /// <summary>
        /// Handles changes to the <see cref="FrameworkElement.ActualWidth"/> property of the
        /// "Faction" <see cref="ListView"/>.</summary>
        /// <param name="sender">
        /// The <see cref="ListView"/> control sending the event.</param>
        /// <param name="args">
        /// An <see cref="EventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnFactionWidthChanged</b> resizes the first column of the "Faction" list view to the
        /// available list view width.</remarks>

        private void OnFactionWidthChanged(object sender, EventArgs args) {
            double width = FactionList.ActualWidth - FactionColorColumn.ActualWidth - 28;
            if (width > 0) FactionColumn.Width = width;
        }

        #endregion
        #endregion
    }
}
