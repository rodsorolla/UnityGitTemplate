using System;
using Sorolla.PersistentData;

namespace Sorolla.Lives
{
    /// <summary>
    /// Persisted lives state. Stored as JSON via Sorolla.PersistentData.SaveSystem
    /// under file name "lives", slot 0. All timestamps are ISO-8601 UTC strings —
    /// see LivesManager for parsing helpers.
    /// </summary>
    [Serializable]
    public class LivesData : ISaveData
    {
        public int Version => 1;

        /// <summary>Current lives. Range 0..LivesConfig.MaxLives.</summary>
        public int current;

        /// <summary>UTC instant when the next life regenerates. Null/empty when at max.</summary>
        public string nextLifeAtUtcIso;

        /// <summary>UTC instant when the active infinite-lives booster expires. Null/empty when no booster.</summary>
        public string boosterUntilUtcIso;

        /// <summary>UTC of last observed system time. Used for backward-jump cheat detection.</summary>
        public string lastSeenUtcIso;
    }
}
