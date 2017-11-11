using System;
using System.Globalization;

using Tektosyne;
using Tektosyne.Collections;

namespace Hexkit.Scenario {
    #region Type Aliases

    using CategorizedValueDictionary = KeyValueList<String, CategorizedValue>;
    using VariableValueDictionary = SortedListEx<String, Int32>;

    #endregion

    /// <summary>
    /// Stores values associated with entity categories.</summary>
    /// <remarks><para>
    /// <b>CategorizedValue</b> provides an <see cref="Int32"/> property for each of the values of
    /// the <see cref="EntityCategory"/> enumeration, an additional property to store a single
    /// uncategorized value, and a read-only property that returns the sum of all other properties.
    /// </para><para>
    /// Use <b>CategorizedValue</b> instead of a plain <see cref="Int32"/> when a value (typically a
    /// variable value) is the sum of other values that are in some way related to entity
    /// categories, and the constituent parts associated with each category should be retained.
    /// </para></remarks>

    public class CategorizedValue {
        #region Effect

        /// <summary>
        /// Gets or sets the value for the <see cref="EntityCategory.Effect"/> category.</summary>
        /// <value>
        /// The component of the <see cref="Total"/> value that is associated with the <see
        /// cref="EntityCategory.Effect"/> category.</value>

        public int Effect { get; set; }

        #endregion
        #region Other

        /// <summary>
        /// Gets or sets the uncategorized value.</summary>
        /// <value>
        /// The component of the <see cref="Total"/> value that is not associated with any entity
        /// category.</value>
        /// <remarks>
        /// Set the <b>Other</b> property to a value other than zero to adjust the value of the <see
        /// cref="Total"/> property while retaining the values of the other properties.</remarks>

        public int Other { get; set; }

        #endregion
        #region Terrain

        /// <summary>
        /// Gets or sets the value for the <see cref="EntityCategory.Terrain"/> category.</summary>
        /// <value>
        /// The component of the <see cref="Total"/> value that is associated with the <see
        /// cref="EntityCategory.Terrain"/> category.</value>

        public int Terrain { get; set; }

        #endregion
        #region Total

        /// <summary>
        /// Gets the sum of all other property values.</summary>
        /// <value>
        /// The sum of the values of the <see cref="Effect"/>, <see cref="Other"/>, <see
        /// cref="Terrain"/>, <see cref="Unit"/>, and <see cref="Upgrade"/> properties.</value>

        public int Total {
            get { return Effect + Other + Terrain + Unit + Upgrade; }
        }

        #endregion
        #region Unit

        /// <summary>
        /// Gets or sets the value for the <see cref="EntityCategory.Unit"/> category.</summary>
        /// <value>
        /// The component of the <see cref="Total"/> value that is associated with the <see
        /// cref="EntityCategory.Unit"/> category.</value>

        public int Unit { get; set; }

        #endregion
        #region Upgrade

        /// <summary>
        /// Gets or sets the value for the <see cref="EntityCategory.Upgrade"/> category.</summary>
        /// <value>
        /// The component of the <see cref="Total"/> value that is associated with the <see
        /// cref="EntityCategory.Upgrade"/> category.</value>

        public int Upgrade { get; set; }

        #endregion
        #region CreateTotalDictionary

        /// <summary>
        /// Creates a new <see cref="VariableValueDictionary"/> from the specified <see
        /// cref="CategorizedValueDictionary"/>.</summary>
        /// <param name="categorizedValues">
        /// A <see cref="CategorizedValueDictionary"/> that maps arbitrary <see cref="String"/> keys
        /// to <see cref="CategorizedValue"/> objects.</param>
        /// <returns>
        /// A <see cref="VariableValueDictionary"/> that maps all <see cref="String"/> keys in the
        /// specified <paramref name="categorizedValues"/> collection to the sum of all
        /// corresponding <see cref="Total"/> values.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="categorizedValues"/> is a null reference.</exception>
        /// <remarks>
        /// <b>CreateTotalDictionary</b> never returns a null reference, but it returns an empty
        /// dictionary if the specified <paramref name="categorizedValues"/> collection is empty.
        /// </remarks>

        public static VariableValueDictionary CreateTotalDictionary(
            CategorizedValueDictionary categorizedValues) {

            if (categorizedValues == null)
                ThrowHelper.ThrowArgumentNullException("categorizedValues");

            var totalValues = new VariableValueDictionary(categorizedValues.Count);

            for (int i = 0; i < categorizedValues.Count; i++) {
                string id = categorizedValues.GetKey(i);
                int value = categorizedValues.GetByIndex(i).Total;

                // add existing value or store new value
                if (totalValues.ContainsKey(id))
                    totalValues[id] += value;
                else
                    totalValues.Add(id, value);
            }

            return totalValues;
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="CategorizedValue"/>
        /// object.</summary>
        /// <returns>
        /// The <see cref="String"/> representation of the value of the <see cref="Total"/>
        /// property.</returns>

        public override string ToString() {
            return Total.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
