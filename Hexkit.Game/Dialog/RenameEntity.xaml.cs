using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Tektosyne;
using Hexkit.Global;
using Hexkit.World;

namespace Hexkit.Game.Dialog {

    /// <summary>
    /// Shows a dialog allowing the user to change the name of a specific <see cref="Entity"/>.
    /// </summary>
    /// <remarks>
    /// Please refer to the "Rename Entity" page of the "Game Dialogs" section in the application
    /// help file for details on this dialog.</remarks>

    public partial class RenameEntity: Window {
        #region RenameEntity(WorldState, Entity)

        /// <summary>
        /// Initializes a new instance of the <see cref="RenameEntity"/> class with the specified
        /// <see cref="WorldState"/> and <see cref="Entity"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> that contains the specified <paramref name="entity"/>.
        /// </param>
        /// <param name="entity">
        /// The <see cref="Entity"/> whose <see cref="Entity.Name"/> to change.</param>
        /// <exception cref="ArgumentException"><para>
        /// The <see cref="Entity.Owner"/> property of the specified <paramref name="entity"/> is a
        /// null reference.
        /// </para><para>-or-</para><para>
        /// The <see cref="WorldState.Entities"/> collection of the specified <paramref
        /// name="worldState"/> does not contain the specified <paramref name="entity"/>.
        /// </para></exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> or <paramref name="entity"/> is a null reference.
        /// </exception>

        public RenameEntity(WorldState worldState, Entity entity) {
            if (worldState == null)
                ThrowHelper.ThrowArgumentNullException("worldState");
            if (entity == null)
                ThrowHelper.ThrowArgumentNullOrEmptyException("collection");

            if (entity.Owner == null)
                ThrowHelper.ThrowArgumentExceptionWithFormat(
                    "entity", Tektosyne.Strings.ArgumentPropertyInvalid, "Owner");

            if (!worldState.Entities.ContainsKey(entity.Id))
                ThrowHelper.ThrowArgumentException(
                    "entity", Tektosyne.Strings.ArgumentNotInCollection);

            this._worldState = worldState;
            this._entity = entity;

            InitializeComponent();
            Title += entity.Name;

            // set focus on name
            NameBox.Text= entity.Name;
            NameBox.Focus();
        }

        #endregion
        #region Private Fields

        // construction parameters
        private readonly WorldState _worldState;
        private readonly Entity _entity;

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
        /// cref="RenameEntity"/> dialog.</remarks>

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs args) {
            args.Handled = true;
            ApplicationUtility.ShowHelp("DlgRenameEntity.html");
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
        /// handles the <see cref="Window.Closing"/> event by by processing the contents of the
        /// "Name" text box.
        /// </para><para>
        /// If the text box is empty or contains the current <see cref="Entity.Name"/> of the <see
        /// cref="Entity"/> being edited, or if the user already cancelled the dialog,
        /// <b>OnClosing</b> sets the <see cref="Window.DialogResult"/> to <c>false</c>.
        /// </para><para>
        /// Otherwise, <b>OnClosing</b> renames the <see cref="Entity"/> being edited to the entered
        /// name via <see cref="Session.Executor"/>.</para></remarks>

        protected override void OnClosing(CancelEventArgs args) {
            base.OnClosing(args);
            if (args.Cancel) return;

            // interpret empty or unchanged input as cancellation
            string name = NameBox.Text;
            if (String.IsNullOrEmpty(name) || name == this._entity.Name)
                DialogResult = false;

            // quit immediately if user cancelled
            if (DialogResult != true)
                return;

            // check if default name requested
            if (name == this._entity.DefaultName)
                name = null;

            // issue Rename command with new entity name
            AsyncAction.Run(() => Session.Instance.Executor.ExecuteRename(
                this._worldState, new List<Entity>(1) { this._entity }, name));
        }

        #endregion
        #region OnDefaultName

        /// <summary>
        /// Handles the <see cref="ButtonBase.Click"/> event for the "Default" <see cref="Button"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="Object"/> where the event handler is attached.</param>
        /// <param name="args">
        /// A <see cref="RoutedEventArgs"/> object containing event data.</param>
        /// <remarks>
        /// <b>OnDefaultName</b> resets the "Name" text box the <see cref="Entity.DefaultName"/>
        /// of the <see cref="Entity"/> being edited.</remarks>

        private void OnDefaultName(object sender, RoutedEventArgs args) {
            args.Handled = true;
            NameBox.Text = this._entity.DefaultName;
        }

        #endregion
    }
}
