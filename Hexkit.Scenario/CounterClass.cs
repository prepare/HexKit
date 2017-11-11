using System;
using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Represents a class of counter variables.</summary>
    /// <remarks>
    /// <b>CounterClass</b> corresponds to the complex XML type "counterClass" defined in <see
    /// cref="FilePaths.ScenarioSchema"/>. Instances are serialized to the XML element "counter".
    /// </remarks>

    public sealed class CounterClass: VariableClass {
        #region CounterClass()

        /// <overloads>
        /// Initializes a new instance of the <see cref="CounterClass"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="CounterClass"/> class with default
        /// properties.</summary>
        /// <remarks>
        /// The <see cref="VariableClass.Category"/> property is initialized to a value of <see
        /// cref="VariableCategory.Counter"/>.</remarks>

        internal CounterClass(): base("", VariableCategory.Counter) { }

        #endregion
        #region CounterClass(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="CounterClass"/> class with the specified
        /// identifier.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="VariableClass.Id"/> property.</param>
        /// <remarks>
        /// The <see cref="VariableClass.Category"/> property is initialized to a value of <see
        /// cref="VariableCategory.Counter"/>.</remarks>

        public CounterClass(string id): base(id, VariableCategory.Counter) { }

        #endregion
        #region CounterClass(CounterClass)

        /// <summary>
        /// Initializes a new instance of the <see cref="CounterClass"/> class with property values
        /// copied from the specified instance.</summary>
        /// <param name="counter">
        /// The <see cref="CounterClass"/> instance whose property values are copied to the new
        /// instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="counter"/> is a null reference.</exception>
        /// <remarks>
        /// This constructor is called by <see cref="Clone"/> to perform a deep copy of the
        /// specified <paramref name="counter"/>, including a deep copy of all mutable objects that
        /// are owned by the <paramref name="counter"/>.</remarks>

        public CounterClass(CounterClass counter): base(counter) { }

        #endregion
        #region ICloneable Members

        /// <summary>
        /// Creates a new <see cref="CounterClass"/> object that is a deep copy of the current
        /// instance.</summary>
        /// <returns>
        /// A new <see cref="CounterClass"/> object that is a deep copy of the current instance.
        /// </returns>
        /// <remarks>
        /// <b>Clone</b> calls the <see cref="CounterClass(CounterClass)"/> copy constructor with
        /// this <see cref="CounterClass"/> object.</remarks>

        public override object Clone() {
            return new CounterClass(this);
        }

        #endregion
        #region XmlSerializable Members
        #region ConstXmlName

        /// <summary>
        /// The name of the XML element associated with the <see cref="CounterClass"/> class.
        /// </summary>
        /// <remarks>
        /// <b>ConstXmlName</b> holds the value "counter", indicating the XML element in <see
        /// cref="FilePaths.ScenarioSchema"/> whose data is managed by the <see
        /// cref="CounterClass"/> class.</remarks>

        public const string ConstXmlName = "counter";

        #endregion
        #endregion
    }
}
