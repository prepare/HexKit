namespace Hexkit.Players {

    /// <summary>
    /// Specifies the possible ways of human player participation.</summary>
    /// <remarks>
    /// <b>GameMode</b> specifies how <see cref="HumanPlayer"/> objects are associated with the <see
    /// cref="World.WorldState.Factions"/> in a <see cref="World.WorldState"/>. This determines how
    /// the game is communicated to a human player when one of his factions has been activated.
    /// </remarks>

    public enum GameMode {
        #region Single

        /// <summary>
        /// Specifies single-player mode.</summary>
        /// <remarks><para>
        /// <b>Single</b> specifies that there are either no human-controlled factions, or that all
        /// such factions are controlled by the same <see cref="HumanPlayer"/>.
        /// </para><para>
        /// In <b>Single</b> mode, Hexkit Game activates all human-controlled factions on the local
        /// system, without requesting confirmation.</para></remarks>

        Single,

        #endregion
        #region Hotseat

        /// <summary>
        /// Specifies "hotseat" multiplayer mode.</summary>
        /// <remarks><para>
        /// <b>Hotseat</b> specifies that there are several different <see cref="HumanPlayer"/>
        /// objects controlling factions, and that all of their <see cref="HumanPlayer.Email"/>
        /// properties are empty strings.
        /// </para><para>
        /// In <b>Hotseat</b> mode, Hexkit Game activates all human-controlled factions on the local
        /// system, but shows the human player's <see cref="Player.Name"/> and requests confirmation
        /// first.</para></remarks>

        Hotseat,

        #endregion
        #region Email

        /// <summary>
        /// Specifies play-by-email multiplayer mode.</summary>
        /// <remarks><para>
        /// <b>Email</b> specifies that there are several different <see cref="HumanPlayer"/>
        /// objects controlling factions, and that all of their <see cref="HumanPlayer.Email"/>
        /// properties are non-empty strings.
        /// </para><para>
        /// In <b>Email</b> mode, Hexkit Game shows the human player's <see cref="Player.Name"/> and
        /// <see cref="HumanPlayer.Email"/> address for all human-controlled factions, and requests
        /// the local user to confirm the player's assumed location.
        /// </para><para>
        /// If the player is local, Hexkit Game proceeds as if in <see cref="Hotseat"/> mode;
        /// otherwise, a PBEM file is created and sent to the remote player's <b>Email</b> address.
        /// </para></remarks>

        Email

        #endregion
    }
}
