namespace Sorolla.Events
{
    /// <summary>Why an active event stopped being active.</summary>
    public enum EventEndReason
    {
        /// <summary>End-at UTC has passed.</summary>
        WindowExpired = 0,
        /// <summary>A new event window started before the old one ended (overlap = replace).</summary>
        Replaced = 1,
        /// <summary>events_enabled remote-config kill switch flipped to false.</summary>
        KillSwitchDisabled = 2,
    }
}
