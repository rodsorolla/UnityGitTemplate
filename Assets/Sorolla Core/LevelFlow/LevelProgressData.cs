using System;
using Sorolla.PersistentData;

namespace Sorolla.LevelFlow
{
    /// <summary>
    /// Persistent data for level and world progress.
    /// Saved automatically when levels are completed.
    /// </summary>
    [Serializable]
    public class LevelProgressData : ISaveData
    {
        public int Version => 1;

        // Level progress (1-based indexing)
        /// <summary>The current level the player is on.</summary>
        public int currentLevel = 1;

        /// <summary>The highest level the player has reached.</summary>
        public int highestLevelReached = 1;

        /// <summary>Total number of levels completed (lifetime).</summary>
        public int totalLevelsCompleted = 0;

        // World progress (1-based indexing, used when world system is enabled)
        /// <summary>The current world the player is in.</summary>
        public int currentWorld = 1;

        /// <summary>The highest world the player has unlocked.</summary>
        public int highestWorldReached = 1;

        /// <summary>Total number of worlds fully completed.</summary>
        public int totalWorldsCompleted = 0;

        // Timestamps
        /// <summary>Unix timestamp of last play session.</summary>
        public long lastPlayedTimestamp;

        /// <summary>
        /// Updates the last played timestamp to now.
        /// </summary>
        public void UpdateLastPlayed()
        {
            lastPlayedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
