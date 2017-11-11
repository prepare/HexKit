using System;
using System.Windows.Controls;
using System.Windows.Media;

using Hexkit.Graphics;
using Hexkit.World;
using Tektosyne;

namespace Hexkit.Game.Dialog {

    /// <summary>
    /// Provides the <see cref="ContentControl.Content"/> of a <see cref="ListViewItem"/> in the
    /// "Faction" <see cref="ListView"/> of the <see cref="ShowRanking"/> dialog.</summary>
    /// <remarks>
    /// <b>RankingListItem</b> provides the column data for all <see cref="ItemsControl.Items"/> in
    /// the "Faction" list view of the <see cref="ShowRanking"/> dialog.</remarks>

    public class RankingListItem {
        #region RankingListItem(Faction)

        /// <summary>
        /// Initializes a new instance of the <see cref="RankingListItem"/> class with the specified
        /// <see cref="World.Faction"/>.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="Faction"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>

        public RankingListItem(Faction faction) {
            if (faction == null)
                ThrowHelper.ThrowArgumentNullException("faction");

            Faction = faction;
            ValueText = "—";
        }

        #endregion
        #region Background

        /// <summary>
        /// Gets the <see cref="Brush"/> for the <see cref="Control.Background"/> of the <see
        /// cref="Faction"/> column.</summary>
        /// <value>
        /// The result of <see cref="MediaObjects.GetGradientBrush"/> for the <see
        /// cref="World.Faction.Color"/> of the associated <see cref="Faction"/>.</value>

        public Brush Background {
            get { return MediaObjects.GetGradientBrush(Faction.Color); }
        }

        #endregion
        #region Faction

        /// <summary>
        /// Gets the <see cref="World.Faction"/> represented by the <see cref="RankingListItem"/>.
        /// </summary>
        /// <value>
        /// The <see cref="World.Faction"/> represented by the <see cref="RankingListItem"/>.
        /// </value>
        /// <remarks>
        /// <b>Faction</b> never returns a null reference.</remarks>

        public Faction Faction { get; private set; }

        #endregion
        #region Rank

        /// <summary>
        /// Gets or sets the rank of the associated <see cref="Faction"/>.</summary>
        /// <value>
        /// An <see cref="Int32"/> value indicating the rank of the associated <see
        /// cref="Faction"/>. The default is zero.</value>

        public int Rank { get; set; }

        #endregion
        #region Value

        /// <summary>
        /// Gets or sets the value which determines the associated <see cref="Rank"/>.</summary>
        /// <value>
        /// A <see cref="Double"/> value which determines the associated <see cref="Rank"/>. The
        /// default is zero.</value>

        public double Value { get; set; }

        #endregion
        #region ValueText

        /// <summary>
        /// Gets or sets the <see cref="String"/> representation of the associated <see
        /// cref="Value"/>.</summary>
        /// <value>
        /// A formatted <see cref="String"/> representation of the associated <see cref="Value"/>.
        /// The default is an em dash (—).</value>

        public string ValueText { get; set; }

        #endregion
    }
}
