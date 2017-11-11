using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Hexkit.Scenario;

namespace Hexkit.Game.Dialog {

    /// <summary>
    /// Provides the <see cref="ContentControl.Content"/> of a <see cref="ListViewItem"/> in any
    /// <see cref="ListView"/> of the <see cref="BuildEntities"/> dialog.</summary>
    /// <remarks>
    /// <b>BuildListItem</b> provides the column data for all <see cref="ItemsControl.Items"/> in
    /// the "Entity Class" and "Resource" list views of the <see cref="BuildEntities"/> dialog.
    /// </remarks>

    public class BuildListItem {
        #region BuildListItem(EntityClass)

        /// <overloads>
        /// Initializes a new instance of the <see cref="BuildListItem"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildListItem"/> class with the specified
        /// <see cref="Scenario.EntityClass"/>.</summary>
        /// <param name="entity">
        /// The initial value for the <see cref="EntityClass"/> property.</param>
        /// <remarks>
        /// The <see cref="ResourceClass"/> property remains a null reference.</remarks>

        public BuildListItem(EntityClass entity) {
            EntityClass = entity;
        }

        #endregion
        #region BuildListItem(ResourceClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildListItem"/> class with the specified
        /// <see cref="Scenario.ResourceClass"/>.</summary>
        /// <param name="resource">
        /// The initial value for the <see cref="ResourceClass"/> property.</param>
        /// <remarks>
        /// The <see cref="EntityClass"/> property remains a null reference.</remarks>

        public BuildListItem(ResourceClass resource) {
            ResourceClass = resource;
        }

        #endregion
        #region Private Fields

        // property backers
        private static LinearGradientBrush _warningBrush;

        #endregion
        #region WarningBrush

        /// <summary>
        /// Gets a <see cref="Brush"/> that highlights insufficient <see cref="CurrentCount"/>
        /// values.</summary>
        /// <value>
        /// A frozen <see cref="LinearGradientBrush"/> whose color ranges from <see
        /// cref="Colors.Red"/> to <see cref="Colors.Transparent"/>.</value>
        /// <remarks>
        /// The <see cref="LinearGradientBrush"/> returned by <b>WarningBrush</b> is allocated on
        /// first access and cached for repeated access.</remarks>

        public static Brush WarningBrush {
            get {
                if (_warningBrush == null) {
                    _warningBrush = new LinearGradientBrush(Colors.Red, Colors.Transparent, 0.0);
                    _warningBrush.Freeze();
                }

                return _warningBrush;
            }
        }

        #endregion
        #region Background

        /// <summary>
        /// Gets the <see cref="Brush"/> for the <see cref="Control.Background"/> of the <see
        /// cref="CurrentCount"/> column.</summary>
        /// <value>
        /// A null reference if <see cref="ResourceClass"/> is a null reference; otherwise, the
        /// value of <see cref="WarningBrush"/> if <see cref="CurrentCount"/> is less than <see
        /// cref="BuildCount"/>; otherwise, <see cref="Brushes.Transparent"/>.</value>
        /// <remarks>
        /// <b>Background</b> higlights <see cref="CurrentCount"/> values that represent
        /// insufficient <see cref="ResourceClass"/> amounts, compared to the required <see
        /// cref="BuildCount"/>.</remarks>

        public Brush Background {
            get {
                if (ResourceClass == null) return null;
                return (CurrentCount < BuildCount ? WarningBrush : Brushes.Transparent);
            }
        }

        #endregion
        #region BuildCount

        /// <summary>
        /// Gets or sets the <see cref="BuildCount"/> of the <see cref="BuildListItem"/>.</summary>
        /// <value>
        /// An <see cref="Int32"/> value indicating the buildable <see cref="EntityClass"/> count,
        /// or the required <see cref="ResourceClass"/> amount. The default is zero.</value>

        public int BuildCount { get; set; }

        #endregion
        #region BuildText

        /// <summary>
        /// Gets or sets the <see cref="String"/> representation of the <see cref="BuildCount"/>
        /// value.</summary>
        /// <value>
        /// A formatted <see cref="String"/> representation of the value of the <see
        /// cref="BuildCount"/> property. The default is a null reference.</value>

        public string BuildText { get; set; }

        #endregion
        #region CurrentCount

        /// <summary>
        /// Gets or sets the <see cref="CurrentCount"/> of the <see cref="BuildListItem"/>.
        /// </summary>
        /// <value>
        /// An <see cref="Int32"/> value indicating the current <see cref="EntityClass"/> count, or
        /// the current <see cref="ResourceClass"/> amount. The default is zero.</value>

        public int CurrentCount { get; set; }

        #endregion
        #region CurrentText

        /// <summary>
        /// Gets or sets the <see cref="String"/> representation of the <see cref="CurrentCount"/>
        /// value.</summary>
        /// <value>
        /// A formatted <see cref="String"/> representation of the value of the <see
        /// cref="CurrentCount"/> property. The default is a null reference.</value>

        public string CurrentText { get; set; }

        #endregion
        #region EntityClass

        /// <summary>
        /// Gets the <see cref="Scenario.EntityClass"/> represented by the <see
        /// cref="BuildListItem"/>.</summary>
        /// <value>
        /// The <see cref="Scenario.EntityClass"/> represented by the <see cref="BuildListItem"/>,
        /// if any. The default is a null reference.</value>
        /// <remarks>
        /// Either <b>EntityClass</b> or <see cref="ResourceClass"/> may be valid, but not both.
        /// </remarks>

        public EntityClass EntityClass { get; private set; }

        #endregion
        #region ResourceClass

        /// <summary>
        /// Gets the <see cref="Scenario.ResourceClass"/> represented by the <see
        /// cref="BuildListItem"/>.</summary>
        /// <value>
        /// The <see cref="Scenario.ResourceClass"/> represented by the <see cref="BuildListItem"/>,
        /// if any. The default is a null reference.</value>
        /// <remarks>
        /// Either <see cref="EntityClass"/> or <b>ResourceClass</b> may be valid, but not both.
        /// </remarks>

        public ResourceClass ResourceClass { get; private set; }

        #endregion
        #region Name

        /// <summary>
        /// Gets a <see cref="String"/> representation of the associated <see cref="EntityClass"/>
        /// or <see cref="ResourceClass"/>.</summary>
        /// <value>
        /// The <see cref="Scenario.EntityClass.Name"/> of the <see cref="EntityClass"/>, if valid;
        /// otherwise, the <see cref="Scenario.VariableClass.Name"/> of the <see
        /// cref="ResourceClass"/>, if valid; otherwise, an em dash (—).</value>

        public string Name {
            get {
                if (EntityClass != null) return EntityClass.Name;
                if (ResourceClass != null) return ResourceClass.Name;
                return "—"; // em dash
            }
        }

        #endregion
    }
}
