using System;
using System.Windows.Controls;
using System.Windows.Media;

using Hexkit.Graphics;

namespace Hexkit.Game {

    /// <summary>
    /// Provides the <see cref="ContentControl.Content"/> of a <see cref="ListViewItem"/> in the
    /// <see cref="PropertyListView"/>.</summary>
    /// <remarks>
    /// <b>PropertyListItem</b> provides the column data for all <see cref="ItemsControl.Items"/> in
    /// a <see cref="PropertyListView"/> control.</remarks>

    public class PropertyListItem {
        #region PropertyListItem()

        /// <overloads>
        /// Initializes a new instance of the <see cref="PropertyListItem"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyListItem"/> class.</summary>

        public PropertyListItem() { }

        #endregion
        #region PropertyListItem(String, String, String, Object)

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyListItem"/> class
        /// with the specified initial property values.</summary>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> property.</param>
        /// <param name="name">
        /// The initial value for the <see cref="Name"/> property.</param>
        /// <param name="value">
        /// The initial value for the <see cref="Value"/> property.</param>
        /// <param name="tag">
        /// The initial value for the <see cref="Tag"/> property.</param>

        public PropertyListItem(string category, string name, string value, object tag) {
            Category = category;
            Name = name;
            Value = value;
            Tag = tag;
        }

        #endregion
        #region Background

        /// <summary>
        /// Gets or sets the <see cref="Brush"/> for the <see cref="Control.Background"/> of the
        /// <see cref="Value"/> column.</summary>
        /// <value>
        /// The <see cref="Brush"/> for the <see cref="Control.Background"/> of the <see
        /// cref="Value"/> column. The default is a null reference.</value>
        /// <remarks>
        /// <b>Background</b> may be set to a <see cref="MediaObjects.DangerFadeBrushes"/> element
        /// to visualize the depletion level of the current <see cref="Value"/>.</remarks>

        public Brush Background { get; set; }

        #endregion
        #region Category

        /// <summary>
        /// Gets or sets the property category of the <see cref="PropertyListItem"/>.</summary>
        /// <value>
        /// The category of the property represented by the <see cref="PropertyListItem"/>. The
        /// default is a null reference.</value>

        public string Category { get; set; }

        #endregion
        #region Name

        /// <summary>
        /// Gets or sets the property name of the <see cref="PropertyListItem"/>.</summary>
        /// <value>
        /// The name of the property represented by the <see cref="PropertyListItem"/>. The default
        /// is a null reference.</value>

        public string Name { get; set; }

        #endregion
        #region Tag

        /// <summary>
        /// Gets or sets the tag object of the <see cref="PropertyListItem"/>.</summary>
        /// <value>
        /// An arbitrary <see cref="Object"/> that is associated with the <see
        /// cref="PropertyListItem"/>. The default is a null reference.</value>

        public object Tag { get; set; }

        #endregion
        #region Value

        /// <summary>
        /// Gets or sets the property value of the <see cref="PropertyListItem"/>.</summary>
        /// <value>
        /// The value of the property represented by the <see cref="PropertyListItem"/>. The default
        /// is a null reference.</value>

        public string Value { get; set; }

        #endregion
    }
}
