using System;
using System.Collections.Generic;
using Sorolla.PersistentData;

namespace Sorolla.PersistentData.Samples
{
    /// <summary>
    /// Example save data class demonstrating how to implement ISaveData.
    /// </summary>
    [Serializable]
    public class ExamplePlayerData : ISaveData
    {
        /// <summary>
        /// Data version. Increment when making breaking changes.
        /// </summary>
        public int Version => 2;

        // Player progress
        public int coins;
        public int level = 1;
        public int experience;

        // Inventory (Newtonsoft.Json handles List<T> well)
        public List<string> inventory = new();

        // Settings
        public float musicVolume = 1f;
        public float sfxVolume = 1f;

        // Stats
        public int totalPlayTime; // in seconds
        public int enemiesDefeated;
        public DateTime lastPlayed;

        // Dictionary support (Unity's JsonUtility doesn't support this, but Newtonsoft does)
        public Dictionary<string, int> achievements = new();
    }
}
