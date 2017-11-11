using System;

namespace Hexkit.Game.Dialog {

    /// <summary>
    /// Specifies the possible display modes for the <see cref="ShowEntities"/> dialog.</summary>
    /// <remarks>
    /// <b>ShowEntitiesMode</b> specifies the possible display modes that may be supplied to the
    /// <see cref="ShowEntities"/> constructor.</remarks>

    [Flags]
    public enum ShowEntitiesMode {
        #region Command

        /// <summary>
        /// Shows command buttons.</summary>
        /// <remarks><para>
        /// <b>Command</b> enables the command buttons of the <see cref="ShowEntities"/> dialog,
        /// allowing the user to rename, place, and destroy entities.
        /// </para><para>
        /// Without this flag, the dialog shows all entities but does not allow the user to issue
        /// any commands.</para></remarks>

        Command = 1,

        #endregion
        #region Site

        /// <summary>
        /// Shows the selected <see cref="World.Site"/>.</summary>
        /// <remarks><para>
        /// <b>Site</b> initializes the <see cref="ShowEntities"/> dialog to the <see
        /// cref="Graphics.MapView.SelectedSite"/> of the default <see cref="Session.MapView"/>.
        /// </para><para>
        /// Without this flag, or if no <see cref="World.Site"/> is currently selected, the dialog
        /// initially shows all entities regardless of their location.</para></remarks>

        Site = 2,

        #endregion
        #region Unplaced

        /// <summary>
        /// Shows unplaced entities only.</summary>
        /// <remarks><para>
        /// <b>Unplaced</b> initializes the <see cref="ShowEntities"/> dialog to show only entities
        /// that are not placed on the map.
        /// </para><para>
        /// Without this flag, the dialog initially shows both placed and unplaced entities. This
        /// flag is ignored if the <see cref="Site"/> flag is also specified.</para></remarks>

        Unplaced = 4

        #endregion
    }
}
