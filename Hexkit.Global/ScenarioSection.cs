using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Specifies the sections of a Hexkit scenario.</summary>
    /// <remarks><para>
    /// <b>ScenarioSection</b> specifies the sections that constitute a Hexkit scenario. Each
    /// section corresponds to an XML element in a scenario description, and to a class in the
    /// <b>Hexkit.Scenario</b> assembly.
    /// </para><para>
    /// The values of <b>ScenarioSection</b> appear in the same order as their corresponding XML
    /// elements in <see cref="FilePaths.ScenarioSchema"/>. This order ensures that all data
    /// required by a subsection is provided by the preceding subsections.</para></remarks>

    public enum ScenarioSection {

        /// <summary>Specifies the Master section.</summary>
        Master,

        /// <summary>Specifies the Images section.</summary>
        Images,

        /// <summary>Specifies the Variables section.</summary>
        Variables,

        /// <summary>Specifies the Entities section.</summary>
        Entities,

        /// <summary>Specifies the Factions section.</summary>
        Factions,

        /// <summary>Specifies the Areas section.</summary>
        Areas
    }
}
