using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Xml;
using Hexkit.Global;

namespace Hexkit.Scenario {
    #region Type Aliases

    using ParagraphList = ListEx<String>;
    using VariableModifierDictionary = SortedListEx<String, VariableModifier>;
    using VariableValueDictionary = SortedListEx<String, Int32>;

    #endregion

    /// <summary>
    /// Manages common data for all variable classes.</summary>
    /// <remarks>
    /// <b>VariableClass</b> corresponds to the complex XML type "variableClass" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>.</remarks>

    public abstract class VariableClass: ScenarioElement, ICloneable, IMutableKeyedValue<String> {
        #region VariableClass(String, VariableCategory)

        /// <overloads>
        /// Initializes a new instance of the <see cref="VariableClass"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="VariableClass"/> class with the specified
        /// identifier and category.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="Id"/> property.</param>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> property.</param>

        protected VariableClass(string id, VariableCategory category) {

            if (id != null) this._id = String.Intern(id);
            this._category = category;
        }

        #endregion
        #region VariableClass(String, VariableCategory, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableClass"/> class with the specified
        /// identifier, category, and display name.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="Id"/> property.</param>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> property.</param>
        /// <param name="name">
        /// The initial value for the <see cref="Name"/> property.</param>

        protected VariableClass(string id,
            VariableCategory category, string name): this(id, category) {

            this._name = name;
        }

        #endregion
        #region VariableClass(VariableClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableClass"/> class with property values
        /// copied from the specified instance.</summary>
        /// <param name="variable">
        /// The <see cref="VariableClass"/> instance whose property values are copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="variable"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="variable"/>, including a deep copy of all mutable objects
        /// that are owned by the <paramref name="variable"/>.</remarks>

        protected VariableClass(VariableClass variable) {
            if (variable == null)
                ThrowHelper.ThrowArgumentNullException("variable");

            this._category = variable._category;
            this._id = variable._id;
            this._name = variable._name;

            this._minimum = variable._minimum;
            this._maximum = variable._maximum;
            this._scale = variable._scale;

            this._paragraphs.AddRange(variable._paragraphs);
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly VariableCategory _category;
        private string _id, _name;
        private int _minimum = 0, _maximum = 1000;
        private int _scale = 1;
        private readonly ParagraphList _paragraphs = new ParagraphList();

        #endregion
        #region AbsoluteMaximum

        /// <summary>
        /// The absolute maximum value for any <see cref="VariableClass"/>.</summary>
        /// <remarks><para>
        /// This value must equal the "maxInclusive" value of the simple type "variableRange"
        /// defined in <see cref="FilePaths.ScenarioSchema"/>.
        /// </para><para>
        /// The <see cref="Maximum"/> property of a given <see cref="VariableClass"/> may further
        /// limit the actual range of legal result values.
        /// </para><para>
        /// <b>AbsoluteMaximum</b> always equals or exceeds the square of <see
        /// cref="SimpleXml.MaxSizeIValue"/> so that two-dimensional map coordinates can be encoded
        /// in a single unrestricted instance value.</para></remarks>

        public const int AbsoluteMaximum = 100000000;

        #endregion
        #region AbsoluteMinimum

        /// <summary>
        /// The absolute minimum value for any <see cref="VariableClass"/>.</summary>
        /// <remarks><para>
        /// This value must equal the "minInclusive" value of the simple type "variableRange"
        /// defined in <see cref="FilePaths.ScenarioSchema"/>.
        /// </para><para>
        /// The <see cref="Minimum"/> property of a given <see cref="VariableClass"/> may further
        /// limit the actual range of legal result values.
        /// </para><para>
        /// <b>AbsoluteMinimum</b> always equals the negative value of <see cref="AbsoluteMaximum"/>
        /// so that any instance value can be stored with either a positive or a negative sign.
        /// </para></remarks>

        public const int AbsoluteMinimum = -100000000;

        #endregion
        #region Category

        /// <summary>
        /// Gets the category of the <see cref="VariableClass"/>.</summary>
        /// <value>
        /// A <see cref="VariableCategory"/> value indicating the category of the <see
        /// cref="VariableClass"/>.</value>
        /// <remarks><para>
        /// <b>Category</b> never changes once the object has been constructed.
        /// </para><para>
        /// The value of this property determines the exact type of this <see cref="VariableClass"/>
        /// object: <see cref="AttributeClass"/>, <see cref="CounterClass"/>, or <see
        /// cref="ResourceClass"/>.</para></remarks>

        public VariableCategory Category {
            [DebuggerStepThrough]
            get { return this._category; }
        }

        #endregion
        #region DecimalPlaces

        /// <summary>
        /// Gets the number of decimal places displayed for the <see cref="VariableClass"/>.
        /// </summary>
        /// <value>
        /// The number of decimal places that are displayed by the <see cref="Format"/> method. The
        /// default is zero.</value>
        /// <remarks><para>
        /// <b>DecimalPlaces</b> returns one of the following values, depending on the current <see
        /// cref="Scale"/>:
        /// </para><list type="table"><listheader>
        /// <term><b>Scale</b></term><description><b>DecimalPlaces</b></description>
        /// </listheader><item>
        /// <term>1</term><description>0</description>
        /// </item><item>
        /// <term>2, 5, 10</term><description>1</description>
        /// </item><item>
        /// <term>Any other value</term><description>2</description>
        /// </item></list></remarks>

        public int DecimalPlaces {
            [DebuggerStepThrough]
            get {
                switch (Scale) {
                    case 1:     return 0;

                    case 2:
                    case 5:
                    case 10:    return 1;

                    default:    return 2;
                }
            }
        }

        #endregion
        #region Id

        /// <summary>
        /// Gets or sets the identifier of the <see cref="VariableClass"/>.</summary>
        /// <value>
        /// The identifier of the <see cref="VariableClass"/>. The default is an empty string.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Id</b> returns an empty string when set to a null reference. This property holds the
        /// value of the "id" XML attribute which must be unique among all identifiers defined by
        /// the scenario.</remarks>

        public string Id {
            [DebuggerStepThrough]
            get { return this._id ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                if (value != null) value = String.Intern(value);
                this._id = value;
            }
        }

        #endregion
        #region Maximum

        /// <summary>
        /// Gets or sets the maximum result value of the <see cref="VariableClass"/>.</summary>
        /// <value>
        /// The maximum result value for the <see cref="VariableClass"/>. The default is 1000.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is less than <see cref="AbsoluteMinimum"/> or
        /// greater than <see cref="AbsoluteMaximum"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Maximum</b> holds the value of the "maximum" attribute of the nested "range" XML
        /// element. This property specifies the upper bound of the result range for any
        /// calculations involving instance values of the <see cref="VariableClass"/>.
        /// </para></remarks>

        public int Maximum {
            [DebuggerStepThrough]
            get { return this._maximum; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                CheckAbsoluteRange(value);
                this._maximum = value;
            }
        }

        #endregion
        #region Minimum

        /// <summary>
        /// Gets or sets the minimum result value of the <see cref="VariableClass"/>.</summary>
        /// <value>
        /// The minimum result value for the <see cref="VariableClass"/>. The default is zero.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is less than <see cref="AbsoluteMinimum"/> or
        /// greater than <see cref="AbsoluteMaximum"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Minimum</b> holds the value of the "minimum" attribute of the nested "range" XML
        /// element. This property specifies the lower bound of the result range for any
        /// calculations involving instance values of the <see cref="VariableClass"/>.
        /// </para></remarks>

        public int Minimum {
            [DebuggerStepThrough]
            get { return this._minimum; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                CheckAbsoluteRange(value);
                this._minimum = value;
            }
        }

        #endregion
        #region Name

        /// <summary>
        /// Gets or sets the display name of the <see cref="VariableClass"/>.</summary>
        /// <value>
        /// The display name of the <see cref="VariableClass"/>. The default is an empty string.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks>
        /// <b>Name</b> returns an empty string when set to a null reference. This property holds
        /// the value of the "name" XML attribute which is used by Hexkit Game when presenting
        /// scenario data to the player.</remarks>

        public string Name {
            [DebuggerStepThrough]
            get { return this._name ?? ""; }
            [DebuggerStepThrough]
            set {
                ApplicationInfo.CheckEditor();
                if (value != null) value = String.Intern(value);
                this._name = value;
            }
        }

        #endregion
        #region Paragraphs

        /// <summary>
        /// Gets a list of paragraphs with additional information about the <see
        /// cref="VariableClass"/>.</summary>
        /// <value>
        /// A <see cref="ParagraphList"/> containing a sequence of paragraphs with additional
        /// information about the <see cref="VariableClass"/>. The default is an empty collection.
        /// </value>
        /// <remarks><para>
        /// <b>Paragraphs</b> never returns a null reference. The collection is read-only if <see
        /// cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </para><para>
        /// This property holds the values of all "para" XML elements. Empty XML elements are stored
        /// as empty strings, and should be displayed as blank lines between paragraphs.
        /// </para></remarks>

        public ParagraphList Paragraphs {
            [DebuggerStepThrough]
            get {
                return (ApplicationInfo.IsEditor ?
                    this._paragraphs : this._paragraphs.AsReadOnly());
            }
        }

        #endregion
        #region Scale

        /// <summary>
        /// Gets or sets the display scale for the <see cref="VariableClass"/>.</summary>
        /// <value>
        /// The positive denominator by which any instance value of the <see cref="VariableClass"/>
        /// is divided before it is displayed using <see cref="Format"/>. The default is one.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is less than one or greater than one hundred.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set, and <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.
        /// </exception>
        /// <remarks><para>
        /// <b>Scale</b> holds the value of the "scale" attribute of the nested "range" XML element.
        /// </para><para>
        /// This property specifies the display scale for instance values of the <see
        /// cref="VariableClass"/>, allowing the representation of variable values as fractional
        /// numbers. Call <see cref="Format"/> to obtain a string representation of a property value
        /// that takes the <b>Scale</b> into account.</para></remarks>

        public int Scale {
            [DebuggerStepThrough]
            get { return this._scale; }
            set {
                ApplicationInfo.CheckEditor();

                if (value < 1 || value > 100)
                    ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat(
                        "value", value, Tektosyne.Strings.ArgumentLessOrGreater, 1, 100);

                this._scale = value;
            }
        }

        #endregion
        #region CheckAbsoluteRange(Int32)

        /// <overloads>
        /// Checks that the specified value falls within the maximum range for any <see
        /// cref="VariableClass"/>.</overloads>
        /// <summary>
        /// Checks that the specified <see cref="Int32"/> value falls within the maximum range for
        /// any <see cref="VariableClass"/>.</summary>
        /// <param name="value">
        /// The potential <see cref="VariableClass"/> value to check.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="value"/> is less than <see cref="AbsoluteMinimum"/> or greater than <see
        /// cref="AbsoluteMaximum"/>.</exception>
        /// <remarks>
        /// <b>CheckAbsoluteRange</b> does nothing if the specified <paramref name="value"/> is
        /// valid.</remarks>

        public static void CheckAbsoluteRange(int value) {
            if (value < AbsoluteMinimum || value > AbsoluteMaximum)
                ThrowHelper.ThrowArgumentOutOfRangeExceptionWithFormat("value", value,
                    Tektosyne.Strings.ArgumentLessOrGreater, AbsoluteMinimum, AbsoluteMaximum);
        }

        #endregion
        #region CheckAbsoluteRange(Nullable<Int32>)

        /// <summary>
        /// Checks that the specified <see cref="Nullable{Int32}"/> value falls within the maximum
        /// range for any <see cref="VariableClass"/>.</summary>
        /// <param name="value">
        /// The potential <see cref="VariableClass"/> value to check.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="value"/> is less than <see cref="AbsoluteMinimum"/> or greater than <see
        /// cref="AbsoluteMaximum"/>.</exception>
        /// <remarks>
        /// <b>CheckAbsoluteRange</b> does nothing if the specified <paramref name="value"/> is
        /// valid or a null reference.</remarks>

        public static void CheckAbsoluteRange(int? value) {
            if (value.HasValue)
                CheckAbsoluteRange(value.Value);
        }

        #endregion
        #region Create

        /// <summary>
        /// Returns a new instance of the <see cref="VariableClass"/> class with the specified
        /// identifier and category.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="Id"/> property.</param>
        /// <param name="category">
        /// The initial value for the <see cref="Category"/> property. This parameter also
        /// determines the type of the returned object.</param>
        /// <returns>
        /// An instance of one of the classes derived from the <see cref="VariableClass"/> class, as
        /// determined by <paramref name="category"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="VariableCategory"/> value.
        /// </exception>
        /// <remarks><para>
        /// <b>Create</b> simply forwards the specified <paramref name="id"/> string to the
        /// constructor of one of the classes derived from the <see cref="VariableClass"/> class, as
        /// determined by the specified <paramref name="category"/>.
        /// </para><para>
        /// Use this method or directly invoke the constructor of the appropriate derived class,
        /// whichever is more convenient.</para></remarks>

        public static VariableClass Create(string id, VariableCategory category) {
            switch (category) {

                case VariableCategory.Attribute: return new AttributeClass(id);
                case VariableCategory.Counter:   return new CounterClass(id);
                case VariableCategory.Resource:  return new ResourceClass(id);

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "category", (int) category, typeof(VariableCategory));
                    return null;
            }
        }

        #endregion
        #region Format(Int32, Boolean)

        /// <overloads>
        /// Returns a <see cref="String"/> that represents the specified instance value of the <see
        /// cref="VariableClass"/>.</overloads>
        /// <summary>
        /// Returns a <see cref="String"/> that represents the specified basic or modifier value of
        /// the <see cref="VariableClass"/>.</summary>
        /// <param name="value">
        /// The <see cref="Int32"/> value to format with the current display <see cref="Scale"/>.
        /// </param>
        /// <param name="isModifier">
        /// <c>true</c> to format <paramref name="value"/> as a modifier value; <c>false</c> to
        /// format <paramref name="value"/> as a basic value.</param>
        /// <returns>
        /// A <see cref="String"/> that represents the specified <paramref name="value"/>, divided
        /// by <see cref="Scale"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="value"/> is less than <see cref="AbsoluteMinimum"/> or greater than <see
        /// cref="AbsoluteMaximum"/>.</exception>
        /// <remarks><para>
        /// <b>Format</b> shows the number of fractional digits indicated by <see
        /// cref="DecimalPlaces"/>. If <paramref name="isModifier"/> is <c>false</c>, the scaled
        /// <paramref name="value"/> is presented without decoration, except that negative values
        /// are preceded by an en dash (Unicode character 2013).
        /// </para><para>
        /// If <paramref name="isModifier"/> is <c>true</c>, the scaled <paramref name="value"/> is
        /// surrounded by parentheses. Positive values are preceded by a plus sign, negative values
        /// by an en dash (Unicode character 2013), and zero values by a plus-minus sign (Unicode
        /// character 00B1).</para></remarks>

        public string Format(int value, bool isModifier) {
            CheckAbsoluteRange(value);

            // scale value for display
            double scaledValue = value;
            if (Scale > 1) scaledValue /= Scale;

            // determine display format
            string format;
            switch (DecimalPlaces) {
                case 0:
                    format = (isModifier ? "(+#,##0);(–#,##0);(±0)" : "#,##0;–#,##0;0");
                    break;

                case 1:
                    format = (isModifier ? "(+#,##0.0);(–#,##0.0);(±0.0)" : "#,##0.0;–#,##0.0;0.0");
                    break;

                default:
                    format = (isModifier ?
                        "(+#,##0.00);(–#,##0.00);(±0.00)" : "#,##0.00;–#,##0.00;0.00");
                    break;
            }

            return scaledValue.ToString(format, ApplicationInfo.Culture);
        }

        #endregion
        #region Format(Nullable<Int32>, ModifierTarget)

        /// <summary>
        /// Returns a <see cref="String"/> that represents the specified modifier value for the
        /// specified <see cref="ModifierTarget"/> and modifier range.</summary>
        /// <param name="value">
        /// The <see cref="Nullable{Int32}"/> value to format with the current display <see
        /// cref="Scale"/>.</param>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating the target of the specified modifier
        /// <paramref name="value"/>.</param>
        /// <param name="range">
        /// The <see cref="EntityClass.ModifierRange"/> of the <see cref="EntityClass"/> that
        /// defines the modifier value.</param>
        /// <returns>
        /// A <see cref="String"/> that represents the specified <paramref name="value"/> and
        /// <paramref name="target"/>, divided by <see cref="Scale"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="value"/> is less than <see cref="AbsoluteMinimum"/> or greater than <see
        /// cref="AbsoluteMaximum"/>.</exception>
        /// <remarks><para>
        /// <b>Format</b> concatenates the localized string representations of the specified
        /// <paramref name="target"/> and of the specified <paramref name="value"/>.
        /// </para><para>
        /// If <paramref name="target"/> equals <see cref="ModifierTarget.UnitsRanged"/> or <see
        /// cref="ModifierTarget.OwnerUnitsRanged"/>, the specified <paramref name="range"/> is
        /// inserted between <paramref name="target"/> and <paramref name="value"/>. An infinity
        /// symbol ("∞", Unicode character 221E) is substituted for a <paramref name="range"/> that
        /// equals zero.
        /// </para><para>
        /// <b>Format</b> uses the same numerical formatting for modifier values as the <see
        /// cref="Format(Int32, Boolean)"/> overload, but without surrounding parentheses. If
        /// <paramref name="value"/> is a null reference, the value zero is shown instead.
        /// </para></remarks>

        public string Format(int? value, ModifierTarget target, int range) {
            CheckAbsoluteRange(value);

            // scale value for display
            double scaledValue = value.GetValueOrDefault();
            if (Scale > 1) scaledValue /= Scale;

            // determine display format
            string format;
            switch (DecimalPlaces) {
                case 0:
                    format = "+#,##0;–#,##0;±0";
                    break;

                case 1:
                    format = "+#,##0.0;–#,##0.0;±0.0";
                    break;

                default:
                    format = "+#,##0.00;–#,##0.00;±0.00";
                    break;
            }

            // insert range if necessary
            switch (target) {

                case ModifierTarget.UnitsRanged:
                case ModifierTarget.OwnerUnitsRanged:
                    string rangeText = (range > 0 ? range.ToString(ApplicationInfo.Culture) : "∞");
                    return String.Format(ApplicationInfo.Culture, "{0} {1}/{2}",
                        VariableModifier.FormatTarget(target), rangeText,
                        scaledValue.ToString(format, ApplicationInfo.Culture));

                default:
                    return String.Format(ApplicationInfo.Culture, "{0} {1}",
                        VariableModifier.FormatTarget(target),
                        scaledValue.ToString(format, ApplicationInfo.Culture));
            }
        }

        #endregion
        #region FormatUnscaled(Int32, Boolean)

        /// <overloads>
        /// Returns a <see cref="String"/> that represents the specified unscaled instance value of
        /// the <see cref="VariableClass"/>.</overloads>
        /// <summary>
        /// Returns a <see cref="String"/> that represents the specified unscaled basic or modifier
        /// value.</summary>
        /// <param name="value">
        /// The <see cref="Int32"/> value to format without display scaling.</param>
        /// <param name="isModifier">
        /// <c>true</c> to format <paramref name="value"/> as a modifier value; <c>false</c> to
        /// format <paramref name="value"/> as a basic value.</param>
        /// <returns>
        /// A <see cref="String"/> that represents the specified <paramref name="value"/>.</returns>
        /// <remarks><para>
        /// <b>FormatUnscaled</b> does not show any fractional digits. If <paramref
        /// name="isModifier"/> is <c>false</c>, the specified <paramref name="value"/> is presented
        /// without decoration, except that negative values are preceded by an en dash (Unicode
        /// character 2013).
        /// </para><para>
        /// If <paramref name="isModifier"/> is <c>true</c>, the specified <paramref name="value"/>
        /// is surrounded by parentheses. Positive values are preceded by a plus sign, negative
        /// values by an en dash (Unicode character 2013), and zero values by a plus-minus sign
        /// (Unicode character 00B1).</para></remarks>

        public static string FormatUnscaled(int value, bool isModifier) {
            string format = (isModifier ? "(+#,##0);(–#,##0);(±0)" : "#,##0;–#,##0;0");
            return value.ToString(format, ApplicationInfo.Culture);
        }

        #endregion
        #region FormatUnscaled(Nullable<Int32>, ModifierTarget)

        /// <summary>
        /// Returns a <see cref="String"/> that represents the specified unscaled modifier value for
        /// the specified <see cref="ModifierTarget"/>.</summary>
        /// <param name="value">
        /// The <see cref="Nullable{Int32}"/> value to format without display scaling.</param>
        /// <param name="target">
        /// A <see cref="ModifierTarget"/> value indicating the target of the specified modifier
        /// <paramref name="value"/>.</param>
        /// <returns>
        /// A <see cref="String"/> that represents the specified <paramref name="value"/> and
        /// <paramref name="target"/>.</returns>
        /// <remarks><para>
        /// <b>FormatUnscaled</b> concatenates the string representations of the specified <paramref
        /// name="target"/> and of the specified <paramref name="value"/>.
        /// </para><para>
        /// <b>FormatUnscaled</b> uses the same numerical formatting for modifier values as the <see
        /// cref="FormatUnscaled(Int32, Boolean)"/> overload, but without surrounding parentheses.
        /// If <paramref name="value"/> is a null reference, the value zero is shown instead.
        /// </para></remarks>

        public static string FormatUnscaled(int? value, ModifierTarget target) {
            return target.ToString() + " " +
                value.GetValueOrDefault().ToString("+#,##0;–#,##0;±0", ApplicationInfo.Culture);
        }

        #endregion
        #region ParseUnscaled(String)

        /// <overloads>
        /// Converts the specified <see cref="String"/> representation of a variable value to its
        /// <see cref="Decimal"/> equivalent.</overloads>
        /// <summary>
        /// Converts the specified <see cref="String"/> representation of a variable value to its
        /// <see cref="Decimal"/> equivalent.</summary>
        /// <param name="text">
        /// The <see cref="String"/> containing the unscaled variable value to convert.</param>
        /// <returns>
        /// The <see cref="Decimal"/> value equivalent to the variable value contained in <paramref
        /// name="text"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="text"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ParseUnscaled</b> returns zero if the specified <paramref name="text"/> is an empty
        /// string, or if the <see cref="Decimal.Parse"/> method of the <see cref="Decimal"/> class
        /// throws a <see cref="FormatException"/>.
        /// </para><para>
        /// If the first and last characters of <paramref name="text"/> are an opening and a closing
        /// parenthesis, respectively, they are stripped before <b>Parse</b> is called.
        /// </para><para>
        /// If the first character of the remaining <paramref name="text"/> is an en dash (Unicode
        /// character 2013) or a plus-minus sign (Unicode character 00B1), the character is changed
        /// into a minus sign or plus sign, respectively, before <b>Parse</b> is called.
        /// </para><para>
        /// If <b>Parse</b> throws an <see cref="OverflowException"/>, <b>ParseUnscaled</b> returns
        /// either <see cref="AbsoluteMinimum"/> or <see cref="AbsoluteMaximum"/>, depending on
        /// whether the first character of the parsed <paramref name="text"/> is a minus sign.
        /// </para></remarks>

        public static decimal ParseUnscaled(string text) {
            bool isModifier;
            return ParseUnscaled(text, out isModifier);
        }

        #endregion
        #region ParseUnscaled(String, Boolean)

        /// <summary>
        /// Converts the specified <see cref="String"/> representation of a variable value to its
        /// <see cref="Decimal"/> equivalent, and indicates whether the value is a modifier value.
        /// </summary>
        /// <param name="text">
        /// The <see cref="String"/> containing the unscaled variable value to convert.</param>
        /// <param name="isModifier">
        /// <c>true</c> if <paramref name="text"/> contains a modifier value; <c>false</c> if
        /// <paramref name="text"/> contains a basic value.</param>
        /// <returns>
        /// The <see cref="Decimal"/> value equivalent to the variable value contained in <paramref
        /// name="text"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="text"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ParseUnscaled</b> sets <paramref name="isModifier"/> to <c>true</c> exactly if the
        /// first and last characters of <paramref name="text"/> are an opening and a closing
        /// parenthesis, respectively.
        /// </para><para>
        /// Please refer to the <see cref="ParseUnscaled(String)"/> overload with a single parameter
        /// for further details.</para></remarks>

        public static decimal ParseUnscaled(string text, out bool isModifier) {
            if (text == null)
                ThrowHelper.ThrowArgumentNullException("text");

            isModifier = false;
            if (text.Length == 0) return 0m;

            // use string builder to avoid copying
            StringBuilder sb = new StringBuilder(text);

            /*
             * Parse recognizes parentheses with NumberStyles.Any,
             * but the enclosed number is interpreted as negative.
             * We don't want that so we have to strip them first.
             */

            // strip modifier parentheses
            if (sb[0] == '(' && sb[sb.Length - 1] == ')') {
                sb.Remove(0, 1);
                sb.Remove(sb.Length - 1, 1);
                isModifier = true;
            }

            // convert en dash to minus sign,
            // and plus-minus sign to plus sign
            if (sb[0] == '–')
                sb[0] = '-';
            else if (sb[0] == '±')
                sb[0] = '+';

            try {
                return Decimal.Parse(sb.ToString(), NumberStyles.Any, ApplicationInfo.Culture);
            }
            catch (FormatException) {
                return 0m;
            }
            catch (OverflowException) {
                return (sb[0] == '-' ? AbsoluteMinimum : AbsoluteMaximum);
            }
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="VariableClass"/>.
        /// </summary>
        /// <returns>
        /// The value of the <see cref="Name"/> property, if it is not an empty string; otherwise,
        /// the value of the <see cref="Id"/> property.</returns>

        public override string ToString() {
            return (Name.Length == 0 ? Id : Name);
        }

        #endregion
        #region TryParseUnscaled

        /// <summary>
        /// Converts the specified <see cref="String"/> representation of a variable modifier to its
        /// <see cref="Decimal"/> equivalent and <see cref="ModifierTarget"/>.</summary>
        /// <param name="text">
        /// The <see cref="String"/> containing the unscaled variable modifier to convert.</param>
        /// <param name="value">
        /// Returns the <see cref="Decimal"/> value equivalent to the variable modifier contained in
        /// <paramref name="text"/> if the conversion succeeded; otherwise, zero.</param>
        /// <param name="target">
        /// Returns the <see cref="ModifierTarget"/> value whose string representation was found at
        /// the start of <paramref name="text"/> if the conversion succeeded; otherwise, <see
        /// cref="ModifierTarget.Self"/>.</param>
        /// <returns>
        /// <c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="text"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>TryParseUnscaled</b> splits the specified <paramref name="text"/> into substrings
        /// separated by whitespace characters. There must be at least two such substrings. The
        /// first is interpreted as the string representation of the returned <paramref
        /// name="target"/>, and the second is passed to <see cref="ParseUnscaled(String, out
        /// Boolean)"/> for conversion into the returned <paramref name="value"/>.
        /// </para><para>
        /// <b>TryParseUnscaled</b> fails if the specified <paramref name="text"/> does not contain
        /// a <see cref="ModifierTarget"/> value and a formatted basic or modifier value, separated
        /// by whitespace. On failure, <paramref name="value"/> and <paramref name="target"/> are
        /// both set to zero.</para></remarks>

        public static bool TryParseUnscaled(string text,
            out decimal value, out ModifierTarget target) {

            if (text == null)
                ThrowHelper.ThrowArgumentNullException("text");

            value = 0m; target = 0;
            string[] words = text.Split(null);
            if (words.Length < 2) return false;

            foreach (ModifierTarget t in VariableModifier.AllModifierTargets) {
                if (words[0] == t.ToString()) {
                    target = t;
                    value = ParseUnscaled(words[1]);
                    return true;
                }
            }

            return false;
        }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="VariableClass"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="VariableClass"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <see cref="VariableClass"/> does not implement <b>Clone</b>. Derived classes must
        /// override this method to call the <see cref="VariableClass(VariableClass)"/> copy
        /// constructor with this instance, and then perform any additional copying operations
        /// required by the derived class.</remarks>

        public abstract object Clone();

        #endregion
        #region IMutableKeyedValue Members

        /// <summary>
        /// Gets the identifier of the <see cref="VariableClass"/>.</summary>
        /// <value>
        /// The value of the <see cref="Id"/> property.</value>

        string IKeyedValue<String>.Key {
            [DebuggerStepThrough]
            get { return Id; }
        }

        /// <summary>
        /// Sets the identifier of the <see cref="VariableClass"/>.</summary>
        /// <param name="key">
        /// The new value for the <see cref="Id"/> property.</param>
        /// <exception cref="InvalidOperationException">
        /// <see cref="ApplicationInfo.IsEditor"/> is <c>false</c>.</exception>

        void IMutableKeyedValue<String>.SetKey(string key) {
            Id = key;
        }

        #endregion
        #region XmlSerializable Members
        #region GetXmlName

        /// <summary>
        /// Returns the name of the XML element associated with the specified <see
        /// cref="VariableCategory"/>.</summary>
        /// <param name="category">
        /// The <see cref="VariableCategory"/> value whose associated XML element name to return.
        /// </param>
        /// <returns>
        /// The <see cref="XmlName"/> of a <see cref="VariableClass"/> object whose <see
        /// cref="Category"/> property equals the specified <paramref name="category"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="category"/> is not a valid <see cref="VariableCategory"/> value.
        /// </exception>

        internal static string GetXmlName(VariableCategory category) {
            switch (category) {

                case VariableCategory.Attribute: return AttributeClass.ConstXmlName;
                case VariableCategory.Counter:   return CounterClass.ConstXmlName;
                case VariableCategory.Resource:  return ResourceClass.ConstXmlName;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "category", (int) category, typeof(VariableCategory));
                    return null;
            }
        }

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="VariableClass"/> object using the specified
        /// <see cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.ScenarioSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void ReadXmlAttributes(XmlReader reader) {

            XmlUtility.ReadAttributeAsString(reader, "id", ref this._id);
            XmlUtility.ReadAttributeAsString(reader, "name", ref this._name);
        }

        #endregion
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="VariableClass"/> object using the specified
        /// <see cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <returns>
        /// <c>true</c> if the current node of the specified <paramref name="reader"/> contained any
        /// matching data; otherwise, <c>false</c>.</returns>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.ScenarioSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override bool ReadXmlElements(XmlReader reader) {
            switch (reader.Name) {

                case "range":
                    XmlUtility.ReadAttributeAsInt32(reader, "minimum", ref this._minimum);
                    XmlUtility.ReadAttributeAsInt32(reader, "maximum", ref this._maximum);
                    XmlUtility.ReadAttributeAsInt32(reader, "scale", ref this._scale);
                    return true;

                case "para": {
                    string element = reader.ReadString();
                    this._paragraphs.Add(element.PackSpace());
                    return true;
                }

                default: return false;
            }
        }

        #endregion
        #region ReadXmlValue(XmlReader, VariableValueDictionary)

        /// <overloads>
        /// Reads XML data into the specified <see cref="VariableClass"/> dictionary using the
        /// specified <see cref="XmlReader"/>.</overloads>
        /// <summary>
        /// Reads XML data into the specified <see cref="VariableValueDictionary"/> using the
        /// specified <see cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <param name="dictionary">
        /// The <see cref="VariableValueDictionary"/> receiving the data read from <paramref
        /// name="reader"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="reader"/> or <paramref name="dictionary"/> is a null reference.
        /// </exception>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.ScenarioSchema"/>.</exception>
        /// <remarks>
        /// <b>ReadXmlValue</b> reads one XML element from the specified <paramref name="reader"/>
        /// and adds its data to the specified <paramref name="dictionary"/>, with an attribute
        /// named "id" containing the key.</remarks>

        internal static void ReadXmlValue(XmlReader reader, VariableValueDictionary dictionary) {
            if (reader == null)
                ThrowHelper.ThrowArgumentNullException("reader");
            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException("dictionary");

            string idRef = reader["id"];
            string element = reader.ReadString();

            if (idRef != null && element != null)
                dictionary.Add(String.Intern(idRef), XmlConvert.ToInt32(element));
        }

        #endregion
        #region ReadXmlValue(XmlReader, VariableModifierDictionary)

        /// <summary>
        /// Reads XML data into the specified <see cref="VariableModifierDictionary"/> using the
        /// specified <see cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <param name="dictionary">
        /// The <see cref="VariableModifierDictionary"/> receiving the data read from <paramref
        /// name="reader"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="reader"/> or <paramref name="dictionary"/> is a null reference.
        /// </exception>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.ScenarioSchema"/>.</exception>
        /// <remarks>
        /// <b>ReadXmlValue</b> reads one XML element from the specified <paramref name="reader"/>
        /// and adds its data to the specified <paramref name="dictionary"/>, with an attribute
        /// named "id" containing the key and another attribute named "target" containing the <see
        /// cref="ModifierTarget"/> to define.</remarks>

        internal static void ReadXmlValue(XmlReader reader, VariableModifierDictionary dictionary) {
            if (reader == null)
                ThrowHelper.ThrowArgumentNullException("reader");
            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException("dictionary");

            // read optional modifier target
            ModifierTarget target = 0;
            reader.ReadAttributeAsEnum("target", ref target);

            string idRef = reader["id"];
            string element = reader.ReadString();

            if (idRef != null && element != null) {
                VariableModifier modifier;
                if (!dictionary.TryGetValue(idRef, out modifier)) {
                    modifier = new VariableModifier();
                    dictionary.Add(idRef, modifier);
                }

                int value = XmlConvert.ToInt32(element);
                modifier.SetByTarget(target, value);
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="VariableClass"/> object that is serialized to
        /// XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {

            writer.WriteAttributeString("id", Id);
            writer.WriteAttributeString("name", Name);
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="VariableClass"/> object that is serialized to
        /// nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {
            Information.WriteXmlParagraphs(writer, Paragraphs);

            // write "range" element if necessary
            if (Minimum != 0 || Maximum != 1000 || Scale != 1) {
                writer.WriteStartElement("range");

                if (Minimum != 0)
                    writer.WriteAttributeString("minimum", XmlConvert.ToString(Minimum));
                if (Maximum != 1000)
                    writer.WriteAttributeString("maximum", XmlConvert.ToString(Maximum));
                if (Scale != 1)
                    writer.WriteAttributeString("scale", XmlConvert.ToString(Scale));

                writer.WriteEndElement();
            }
        }

        #endregion
        #region WriteXmlValues(XmlWriter, String, VariableValueDictionary)

        /// <overloads>
        /// Writes the contents of the specified <see cref="VariableClass"/> dictionary to the
        /// specified <see cref="XmlWriter"/> using the specified XML element name.</overloads>
        /// <summary>
        /// Writes the contents of the specified <see cref="VariableValueDictionary"/> to the
        /// specified <see cref="XmlWriter"/> using the specified XML element name.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <param name="name">
        /// The name of the XML element to write for each item in the specified <paramref
        /// name="dictionary"/>.</param>
        /// <param name="dictionary">
        /// The <see cref="VariableValueDictionary"/> containing the data to write to <paramref
        /// name="writer"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="writer"/> or <paramref name="dictionary"/> is a null reference.
        /// </exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="name"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// <b>WriteXmlValues</b> writes an XML element named <paramref name="name"/> to the
        /// specified <see cref="XmlWriter"/> for each item in the specified <paramref
        /// name="dictionary"/>, with an attribute named "id" containing the key.</remarks>

        internal static void WriteXmlValues(XmlWriter writer,
            string name, VariableValueDictionary dictionary) {

            if (writer == null)
                ThrowHelper.ThrowArgumentNullException("writer");
            if (String.IsNullOrEmpty(name))
                ThrowHelper.ThrowArgumentNullOrEmptyException("name");
            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException("dictionary");

            foreach (var pair in dictionary) {
                writer.WriteStartElement(name);

                writer.WriteAttributeString("id", pair.Key);
                writer.WriteString(XmlConvert.ToString(pair.Value));

                writer.WriteEndElement();
            }
        }

        #endregion
        #region WriteXmlValues(XmlWriter, String, VariableModifierDictionary)

        /// <summary>
        /// Writes the contents of the specified <see cref="VariableModifierDictionary"/> to the
        /// specified <see cref="XmlWriter"/> using the specified XML element name.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <param name="name">
        /// The name of the XML element to write for each item in the specified <paramref
        /// name="dictionary"/>.</param>
        /// <param name="dictionary">
        /// The <see cref="VariableModifierDictionary"/> containing the data to write to <paramref
        /// name="writer"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="writer"/> or <paramref name="dictionary"/> is a null reference.
        /// </exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="name"/> is a null reference or an empty string.</exception>
        /// <remarks>
        /// <b>WriteXmlValues</b> writes an XML element named <paramref name="name"/> to the
        /// specified <see cref="XmlWriter"/> for each valid <see cref="ModifierTarget"/> defined by
        /// each item in the specified <paramref name="dictionary"/>, with an attribute named "id"
        /// containing the key and another attribute named "target" containing each valid <see
        /// cref="ModifierTarget"/>.</remarks>

        internal static void WriteXmlValues(XmlWriter writer,
            string name, VariableModifierDictionary dictionary) {

            if (writer == null)
                ThrowHelper.ThrowArgumentNullException("writer");
            if (String.IsNullOrEmpty(name))
                ThrowHelper.ThrowArgumentNullOrEmptyException("name");
            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException("dictionary");

            foreach (var pair in dictionary)
                foreach (ModifierTarget target in VariableModifier.AllModifierTargets) {
                    int value = pair.Value.GetByTarget(target).GetValueOrDefault();
                    if (value == 0) continue;

                    writer.WriteStartElement(name);
                    writer.WriteAttributeString("id", pair.Key);
                    writer.WriteAttributeString("target", target.ToString());
                    writer.WriteString(XmlConvert.ToString(value));
                    writer.WriteEndElement();
                }
        }

        #endregion
        #endregion
    }
}
