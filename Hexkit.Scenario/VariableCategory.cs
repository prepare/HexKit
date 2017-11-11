using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Specifies the possible categories of variable classes.</summary>
    /// <remarks>
    /// <b>VariableCategory</b> specifies the categories that correspond to each of the classes
    /// derived from <see cref="VariableClass"/>, as well as to each of the XML elements in <see
    /// cref="FilePaths.ScenarioSchema"/> derived from the complex type "variableClass".</remarks>

    public enum VariableCategory: byte {

        /// <summary>
        /// Specifies the category represented by <see cref="AttributeClass"/>.</summary>

        Attribute,

        /// <summary>
        /// Specifies the category represented by <see cref="CounterClass"/>.</summary>

        Counter,

        /// <summary>
        /// Specifies the category represented by <see cref="ResourceClass"/>.</summary>

        Resource
    }
}
