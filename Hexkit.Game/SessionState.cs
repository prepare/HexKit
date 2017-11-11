namespace Hexkit.Game {

    /// <summary>
    /// Specifies the possible states of a Hexkit game session.</summary>
    /// <remarks><para>
    /// <b>SessionState</b> specifies the possible values for the <see cref="Session.State"/>
    /// property of the <see cref="Session"/> class.
    /// </para><para>
    /// Different states generally correspond to a different set of commands available to the user.
    /// A special <see cref="SessionState.Invalid"/> value indicates that no game session exists.
    /// </para></remarks>

    public enum SessionState {
        #region Invalid

        /// <summary>
        /// Specifies that no valid game session exists.</summary>
        /// <remarks>
        /// <see cref="Session.State"/> equals <b>Invalid</b> if the current <see cref="Session"/>
        /// instance is a null reference or contains invalid data.</remarks>

        Invalid,

        #endregion
        #region Closed

        /// <summary>
        /// Specifies that the game has ended. Human players may examine the situation but no
        /// further input is possible.</summary>
        /// <remarks>
        /// <see cref="Session.State"/> changes to <b>Closed</b> when a victory condition has been
        /// met, and retains this value while the current <see cref="Session"/> instance exists.
        /// </remarks>

        Closed,

        #endregion
        #region Command

        /// <summary>
        /// Specifies that the game is executing a local human player's commands.</summary>
        /// <remarks>
        /// <see cref="Session.State"/> changes from <see cref="Human"/> or <see cref="Selection"/>
        /// to <b>Command</b> in order to execute an entered command, and then back to <see
        /// cref="Human"/>.</remarks>

        Command,

        #endregion
        #region Computer

        /// <summary>
        /// Specifies that the game is awaiting a computer player's commands.</summary>
        /// <remarks>
        /// Once the computer player has created all its commands, <see cref="Session.State"/>
        /// changes to <see cref="Replay"/> to display them on the map view.</remarks>

        Computer,

        #endregion
        #region Human

        /// <summary>
        /// Specifies that the game is awaiting a local human player's commands.</summary>
        /// <remarks>
        /// <see cref="Session.State"/> changes from <b>Human</b> to any of the other states, and
        /// possibly back again, in response to input by the local human player.</remarks>

        Human,

        #endregion
        #region Replay

        /// <summary>
        /// Specifies that an interactive game replay is in progress.</summary>
        /// <remarks>
        /// <see cref="Session.State"/> changes to <b>Replay</b> either from <see cref="Human"/>,
        /// due to a menu command by the local human player, or from <see cref="Computer"/>, when
        /// the active computer player has finished creating its commands.</remarks>

        Replay,

        #endregion
        #region Selection

        /// <summary>
        /// Specifies that the game is awaiting a local human player's target selection.</summary>
        /// <remarks>
        /// <see cref="Session.State"/> changes from <see cref="Human"/> to <b>Selection</b> when
        /// the local human player enters a game command that requires the selection of a target on
        /// the default map view.</remarks>

        Selection

        #endregion
    }
}
