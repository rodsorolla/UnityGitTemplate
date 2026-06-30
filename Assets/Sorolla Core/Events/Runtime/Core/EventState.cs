namespace Sorolla.Events
{
    /// <summary>Lifecycle state of a single event instance from the player's perspective.</summary>
    public enum EventState
    {
        /// <summary>Event not in the catalog or its window has not started.</summary>
        Inactive = 0,
        /// <summary>Window is open but the player has not reached the unlock level.</summary>
        Locked = 1,
        /// <summary>Window is open and the player can earn progress.</summary>
        Active = 2,
        /// <summary>All step thresholds crossed; Grand Prize awaits or has been granted.</summary>
        GrandPrizeReady = 3,
        /// <summary>Window has fully passed.</summary>
        Ended = 4,
    }
}
