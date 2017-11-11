using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;

using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Represents a victory or defeat condition for a <see cref="FactionClass"/>.</summary>
    /// <remarks><para>
    /// <b>Condition</b> specifies a game parameter and a threshold value whose combination triggers
    /// the victory or defeat of the associated <see cref="FactionClass"/>.
    /// </para><para>
    /// <b>Condition</b> only represents victory and defeat conditions that are based on <see
    /// cref="ConditionParameter"/> values. Conditions that are based on <see cref="ResourceClass"/>
    /// values are defined by the corresponding <see cref="ResourceClass"/> itself.</para></remarks>

    public struct Condition: IEquatable<Condition>, IKeyedValue<ConditionParameter> {
        #region Condition(ConditionParameter, Int32)

        /// <summary>
        /// Initializes a new instance of the <see cref="Condition"/> class with the specified game
        /// parameter and threshold value.</summary>
        /// <param name="parameter">
        /// The initial value for the <see cref="Parameter"/> property.</param>
        /// <param name="threshold">
        /// The initial value for the <see cref="Threshold"/> property.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="threshold"/> is less than zero.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="parameter"/> is not a valid <see cref="ConditionParameter"/> value.
        /// </exception>

        public Condition(ConditionParameter parameter, int threshold) {

            if (parameter < ConditionParameter.Sites || parameter > ConditionParameter.Turns)
                ThrowHelper.ThrowInvalidEnumArgumentException(
                    "parameter", (int) parameter, typeof(ConditionParameter));

            if (threshold < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "threshold", threshold, Tektosyne.Strings.ArgumentNegative);

            this._parameter = parameter;
            this._threshold = threshold;
        }

        #endregion
        #region Private Fields

        // property backers
        private ConditionParameter _parameter;
        private int _threshold;

        #endregion
        #region Parameter

        /// <summary>
        /// Gets the game parameter that triggers the <see cref="Condition"/>. </summary>
        /// <value>
        /// A <see cref="ConditionParameter"/> value indicating the game parameter whose <see
        /// cref="Threshold"/> value triggers the <see cref="Condition"/>.</value>
        /// <remarks>
        /// <b>Parameter</b> holds the value of the "parameter" XML attribute.</remarks>

        public ConditionParameter Parameter {
            [DebuggerStepThrough]
            get { return this._parameter; }
        }

        #endregion
        #region Threshold

        /// <summary>
        /// Gets the threshold value that triggers the <see cref="Condition"/>.</summary>
        /// <value>
        /// A non-negative <see cref="Int32"/> value of the associated <see cref="Parameter"/> that
        /// triggers the <see cref="Condition"/>.</value>
        /// <remarks><para>
        /// <b>Threshold</b> holds the value of the "threshold" XML attribute.
        /// </para><para>
        /// If the associated <see cref="Parameter"/> equals <see cref="ConditionParameter.Turns"/>,
        /// the <see cref="Condition"/> is met if the current turn index exceeds <b>Threshold</b>.
        /// </para><para>
        /// Otherwise, the <see cref="Condition"/> is met if the value of the associated <see
        /// cref="Parameter"/> is greater than or equal to <b>Threshold</b> in the case of victory
        /// conditions, and less than or equal to <b>Threshold</b> in the case of defeat conditions.
        /// </para></remarks>

        public int Threshold {
            [DebuggerStepThrough]
            get { return this._threshold; }
        }

        #endregion
        #region GetHashCode

        /// <summary>
        /// Returns the hash code for this <see cref="Condition"/> instance.</summary>
        /// <returns>
        /// An <see cref="Int32"/> hash code.</returns>
        /// <remarks>
        /// <b>GetHashCode</b> returns the value of the <see cref="Threshold"/> property.</remarks>

        public override int GetHashCode() {
            return Threshold;
        }

        #endregion
        #region GetParameterString

        /// <summary>
        /// Returns a localized name for the specified <see cref="ConditionParameter"/>.</summary>
        /// <param name="parameter">
        /// The <see cref="ConditionParameter"/> value whose name to retrieve.</param>
        /// <returns>
        /// A localized name for the specified <paramref name="parameter"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="parameter"/> is not a valid <see cref="ConditionParameter"/> value.
        /// </exception>

        public static string GetParameterString(ConditionParameter parameter) {
            switch (parameter) {

                case ConditionParameter.Sites: return Global.Strings.LabelSites;
                case ConditionParameter.Turns: return Global.Strings.LabelTurns;
                case ConditionParameter.Units: return Global.Strings.LabelUnits;
                case ConditionParameter.UnitStrength: return Global.Strings.LabelUnitStrength;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "parameter", (int) parameter, typeof(ConditionParameter));
                    return null;
            }
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="Condition"/>.</summary>
        /// <returns>
        /// A <see cref="String"/> containing the values of the <see cref="Parameter"/> and <see
        /// cref="Threshold"/> properties.</returns>

        public override string ToString() {
            return String.Format(CultureInfo.InvariantCulture,
                "{{Parameter={0}, Threshold={1}}}", Parameter, Threshold);
        }

        #endregion
        #region Public Operators
        #region operator==

        /// <summary>
        /// Determines whether two <see cref="Condition"/> instances have the same value.</summary>
        /// <param name="x">
        /// The first <see cref="Condition"/> to compare.</param>
        /// <param name="y">
        /// The second <see cref="Condition"/> to compare.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="x"/> is the same as the value of <paramref
        /// name="y"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This operator invokes the <see cref="Equals(Condition)"/> method to test the two <see
        /// cref="Condition"/> instances for value equality.</remarks>

        public static bool operator ==(Condition x, Condition y) {
            return x.Equals(y);
        }

        #endregion
        #region operator!=

        /// <summary>
        /// Determines whether two <see cref="Condition"/> instances have different values.
        /// </summary>
        /// <param name="x">
        /// The first <see cref="Condition"/> to compare.</param>
        /// <param name="y">
        /// The second <see cref="Condition"/> to compare.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="x"/> is different from the value of
        /// <paramref name="y"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This operator invokes the <see cref="Equals(Condition)"/> method to test the two <see
        /// cref="Condition"/> instances for value inequality.</remarks>

        public static bool operator !=(Condition x, Condition y) {
            return !x.Equals(y);
        }

        #endregion
        #endregion
        #region IEquatable Members
        #region Equals(Object)

        /// <overloads>
        /// Determines whether two <see cref="Condition"/> instances have the same value.
        /// </overloads>
        /// <summary>
        /// Determines whether this <see cref="Condition"/> instance and a specified object, which
        /// must be a <see cref="Condition"/>, have the same value.</summary>
        /// <param name="obj">
        /// An <see cref="Object"/> to compare to this <see cref="Condition"/> instance.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> is another <see cref="Condition"/> instance and
        /// its value is the same as this instance; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// If the specified <paramref name="obj"/> is another <see cref="Condition"/> instance,
        /// <b>Equals</b> invokes the strongly-typed <see cref="Equals(Condition)"/> overload to
        /// test the two instances for value equality.</remarks>

        public override bool Equals(object obj) {

            if (obj == null || !(obj is Condition))
                return false;

            return Equals((Condition) obj);
        }

        #endregion
        #region Equals(Condition)

        /// <summary>
        /// Determines whether this instance and a specified <see cref="Condition"/> have the same
        /// value.</summary>
        /// <param name="condition">
        /// A <see cref="Condition"/> to compare to this instance.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="condition"/> is the same as this instance;
        /// otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>Equals</b> compares the values of the <see cref="Parameter"/> and <see
        /// cref="Threshold"/> properties of the two <see cref="Condition"/> instances to test for
        /// value equality.</remarks>

        public bool Equals(Condition condition) {
            return (Parameter == condition.Parameter && Threshold == condition.Threshold);
        }

        #endregion
        #region Equals(Condition, Condition)

        /// <summary>
        /// Determines whether two specified <see cref="Condition"/> instances have the same value.
        /// </summary>
        /// <param name="x">
        /// The first <see cref="Condition"/> to compare.</param>
        /// <param name="y">
        /// The second <see cref="Condition"/> to compare.</param>
        /// <returns>
        /// <c>true</c> if the value of <paramref name="x"/> is the same as the value of <paramref
        /// name="y"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <b>Equals</b> invokes the non-static <see cref="Equals(Condition)"/> overload to test
        /// the two <see cref="Condition"/> instances for value equality.</remarks>

        public static bool Equals(Condition x, Condition y) {
            return x.Equals(y);
        }

        #endregion
        #endregion
        #region IKeyedValue Members

        /// <summary>
        /// Gets the game parameter that triggers the <see cref="Condition"/>.</summary>
        /// <value>
        /// The value of the <see cref="Parameter"/> property.</value>

        ConditionParameter IKeyedValue<ConditionParameter>.Key {
            [DebuggerStepThrough]
            get { return Parameter; }
        }

        #endregion
        #region XmlSerializable Members
        #region ReadXml

        /// <summary>
        /// Reads XML data into the <see cref="Condition"/> object using the specified <see
        /// cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.ScenarioSchema"/>.</exception>
        /// <remarks>
        /// <b>ReadXml</b> performs the same task as the <see cref="XmlSerializable.ReadXml"/>
        /// method of the <see cref="XmlSerializable"/> class.</remarks>

        internal void ReadXml(XmlReader reader) {

            XmlUtility.ReadAttributeAsEnum(reader, "parameter", ref this._parameter);
            XmlUtility.ReadAttributeAsInt32(reader, "threshold", ref this._threshold);
        }

        #endregion
        #region WriteXml

        /// <summary>
        /// Writes the current data of the <see cref="Condition"/> object to the specified <see
        /// cref="XmlWriter"/> with the specified XML element name.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <param name="name">
        /// The <see cref="XmlElement.Name"/> of the XML element to write.</param>
        /// <remarks>
        /// <b>WriteXml</b> performs the same task as the <see cref="XmlSerializable.WriteXml"/>
        /// method of the <see cref="XmlSerializable"/> class.</remarks>

        internal void WriteXml(XmlWriter writer, string name) {
            writer.WriteStartElement(name);

            writer.WriteAttributeString("parameter", Parameter.ToString());
            writer.WriteAttributeString("threshold", XmlConvert.ToString(Threshold));

            writer.WriteEndElement();
        }

        #endregion
        #endregion
    }
}
