using System;

namespace Hexkit.Options {

    /// <summary>
    /// Specifies the display mode for variable values on the map view.</summary>
    /// <remarks><para>
    /// <b>VariableDisplay</b> specifies the possible values for the <see
    /// cref="MapViewOptions.ShownVariableFlags"/> property of the <see cref="MapViewOptions"/>
    /// class.
    /// </para><para>
    /// Any bitwise combination of <b>VariableDisplay</b> flags is legal, but the display will be
    /// disabled unless at least one of the <b>Basic</b> and <b>Modifier</b> flags and one of the
    /// <b>Numbers</b> and <b>Shades</b> flags is set.</para></remarks>

    [Flags]
    public enum VariableDisplay {

        /// <summary>Specifies that basic values are displayed.</summary>
        Basic = 1,

        /// <summary>Specifies that modifier values are displayed.</summary>
        Modifier = 2,

        /// <summary>Specifies that both basic and modifier values are displayed.</summary>
        BasicAndModifier = Basic | Modifier,

        /// <summary>Specifies that values are displayed as numbers.</summary>
        Numbers = 4,

        /// <summary>Specifies that values are displayed as shades.</summary>
        Shades = 8,

        /// <summary>Specifies that values are displayed both as numbers and as shades.</summary>
        NumbersAndShades = Numbers | Shades,
    }
}
