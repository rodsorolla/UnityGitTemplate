namespace Sorolla.LevelFlow
{
    /// <summary>
    /// Represents the current state of level gameplay.
    /// </summary>
    public enum LevelState
    {
        /// <summary>Not currently in a level.</summary>
        Idle,

        /// <summary>Level is being set up (loading, spawning, etc.).</summary>
        Initializing,

        /// <summary>Active gameplay in progress.</summary>
        Playing,

        /// <summary>Gameplay is paused.</summary>
        Paused,

        /// <summary>Level completed successfully.</summary>
        Won,

        /// <summary>Level failed.</summary>
        Lost
    }
}
