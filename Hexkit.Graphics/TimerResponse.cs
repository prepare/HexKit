namespace Hexkit.Graphics {

    /// <summary>
    /// Specifies the response to timer events.</summary>
    /// <remarks>
    /// <b>TimerResponse</b> indicates how an event handler should respond to a timer event. Choices
    /// include normal processing, skipping a single event, and skipping all events.</remarks>

    public enum TimerResponse {

        /// <summary>
        /// All timer events are handled normally.</summary>

        Normal,

        /// <summary>
        /// The next timer event is skipped but subsequent events are handled normally.</summary>

        SkipOne,

        /// <summary>
        /// All timer events are skipped until further notice.</summary>

        Suspend
    }
}
