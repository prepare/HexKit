namespace Hexkit.Global {
    #region Enum ArgumentFileType

    /// <summary>
    /// Specifies the file types recognized by <see cref="FilePaths.FindArgumentFile"/>.</summary>

    public enum ArgumentFileType {

        /// <summary>Specifies an unrecognized or inaccessible file.</summary>
        Invalid,

        /// <summary>Specifies a file that could not be located.</summary>
        Missing,

        /// <summary>Specifies a Hexkit scenario file.</summary>
        Scenario,

        /// <summary>Specifies a Hexkit session file.</summary>
        Session
    }

    #endregion
    #region Enum ScenarioFileType

    /// <summary>
    /// Specifies the predefined file types associated with scenario management.</summary>

    public enum ScenarioFileType {

        /// <summary>
        /// Specifies the optional scenario debug file.</summary>
        /// <remarks>
        /// <b>Debug</b> indicates a file located below <see cref="FilePaths.UserFolder"/> that
        /// Hexkit can create from the current scenario data to aid debugging.</remarks>

        Debug,

        /// <summary>
        /// Specifies the default scenario rule script file.</summary>
        /// <remarks>
        /// <b>Rules</b> indicates the default rule script file located below <see
        /// cref="FilePaths.RulesFolder"/> that provides no functionality beyond the default methods
        /// defined in the <b>Hexkit.World</b> assembly.</remarks>

        Rules,

        /// <summary>
        /// Specifies the temporary scenario file created during startup.</summary>
        /// <remarks>
        /// <b>Start</b> indicates the monolithic XML file located below <see
        /// cref="FilePaths.UserFolder"/> into which all section files that comprise a Hexkit
        /// scenario are assembled at the start of a game. This file is deleted once a scenario has
        /// been started successfully, but otherwise retained to aid debugging.</remarks>

        Start
    }

    #endregion
    #region Enum SessionFileType

    /// <summary>
    /// Specifies the predefined file types associated with session management.</summary>

    public enum SessionFileType {

        /// <summary>
        /// Specifies the session file for automatically saved games for the most recent local human
        /// player.</summary>
        /// <remarks>
        /// <b>Auto</b> indicates a file located below <see cref="FilePaths.SessionFolder"/> that is
        /// automatically created whenever a human player ends his turn on the local machine.
        /// </remarks>

        Auto,

        /// <summary>
        /// Specifies the session file for automatically saved games for the most recent computer
        /// player.</summary>
        /// <remarks>
        /// <b>Computer</b> indicates a file located below <see cref="FilePaths.SessionFolder"/>
        /// that is automatically created whenever a computer player ends its turn.</remarks>

        Computer,

        /// <summary>
        /// Specifies the optional session debug file.</summary>
        /// <remarks>
        /// <b>Debug</b> indicates a file located below <see cref="FilePaths.UserFolder"/> that
        /// Hexkit Game can create from the current session data to aid debugging.</remarks>

        Debug,

        /// <summary>
        /// Specifies the session file for games saved for e-mail transmission.</summary>
        /// <remarks>
        /// <b>Email</b> indicates a file located below <see cref="FilePaths.SessionFolder"/> that
        /// is automatically created when the active faction is controlled by a human player on a
        /// remote machine.</remarks>

        Email
    }

    #endregion
}
