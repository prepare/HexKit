using System;

namespace Hexkit.World {

    /// <summary>
    /// Specifies the purpose of variables and variable containers.</summary>
    /// <remarks><para>
    /// <b>VariablePurpose</b> defines the common purpose of all <see cref="Variable"/> objects
    /// managed by a given <see cref="VariableContainer"/>. The following table lists the valid
    /// bitwise combinations of <b>VariablePurpose</b> values:
    /// </para><list type="table"><listheader>
    /// <term>Combination</term><description>Description</description>
    /// </listheader><item>
    /// <term><b>Basic</b> | <b>Entity</b></term>
    /// <description>Basic value associated with an <see cref="Entity"/>.</description>
    /// </item><item>
    /// <term><b>Basic</b> | <b>Faction</b></term>
    /// <description>Basic value associated with a <see cref="Faction"/>.</description>
    /// </item><item>
    /// <term><b>Modifier</b> | <b>Entity</b></term>
    /// <description>Modifier value associated with an <see cref="Entity"/>.</description>
    /// </item><item>
    /// <term><b>Modifier</b> | <b>Faction</b></term>
    /// <description>Modifier value associated with a <see cref="Faction"/>.</description>
    /// </item></list><para>
    /// Additionally, the <b>Scenario</b> value can be combined with any of these four combinations
    /// to indicate that a value was defined by the scenario, as opposed to the rule script.
    /// </para></remarks>

    [Flags]
    public enum VariablePurpose: byte {

        /// <summary>Specifies an <see cref="Entity"/> variable.</summary>
        Entity = 1,

        /// <summary>Specifies a <see cref="Faction"/> variable.</summary>
        Faction = 2,

        /// <summary>Specifies a basic value.</summary>
        Basic = 4,

        /// <summary>Specifies a modifier value.</summary>
        Modifier = 8,

        /// <summary>Specifies a value defined by the scenario.</summary>
        Scenario = 16,
    }
}
