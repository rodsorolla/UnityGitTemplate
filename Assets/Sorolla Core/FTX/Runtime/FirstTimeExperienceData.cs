using System;
using System.Collections.Generic;
using Sorolla.PersistentData;

namespace Sorolla.FTX
{
    /// <summary>
    /// Persistent data for the First Time Experience system.
    /// Tracks which features/hints have been seen.
    /// </summary>
    [Serializable]
    public class FirstTimeExperienceData : ISaveData
    {
        public int Version => 1;

        /// <summary>
        /// Set of keys that have been seen.
        /// Using List for serialization (HashSet doesn't serialize well with JSON).
        /// </summary>
        public List<string> SeenKeys = new();

        /// <summary>
        /// Checks if a key has been seen.
        /// </summary>
        public bool HasSeen(string key)
        {
            return SeenKeys.Contains(key);
        }

        /// <summary>
        /// Marks a key as seen.
        /// </summary>
        /// <returns>True if newly added, false if already existed</returns>
        public bool MarkAsSeen(string key)
        {
            if (SeenKeys.Contains(key))
                return false;

            SeenKeys.Add(key);
            return true;
        }

        /// <summary>
        /// Removes a key from seen list.
        /// </summary>
        public bool ResetKey(string key)
        {
            return SeenKeys.Remove(key);
        }

        /// <summary>
        /// Clears all seen keys.
        /// </summary>
        public void ResetAll()
        {
            SeenKeys.Clear();
        }
    }
}
