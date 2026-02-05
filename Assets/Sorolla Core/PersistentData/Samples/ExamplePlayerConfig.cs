using UnityEngine;
using Sorolla.PersistentData;

namespace Sorolla.PersistentData.Samples
{
    /// <summary>
    /// Example ScriptableObject that provides default values for ExamplePlayerData.
    /// Create an asset: Assets > Create > Sorolla > Example Player Config
    /// </summary>
    [CreateAssetMenu(fileName = "ExamplePlayerConfig", menuName = "Sorolla/Example Player Config")]
    public class ExamplePlayerConfig : ScriptableObject, IDefaultsProvider<ExamplePlayerData>
    {
        [Header("Starting Values")]
        public int startingCoins = 100;
        public int startingLevel = 1;

        [Header("Default Settings")]
        [Range(0, 1)] public float defaultMusicVolume = 0.8f;
        [Range(0, 1)] public float defaultSfxVolume = 1f;

        [Header("Starting Inventory")]
        public string[] startingItems = { "sword_basic", "potion_health" };

        /// <summary>
        /// Creates a new ExamplePlayerData with values from this config.
        /// </summary>
        public ExamplePlayerData CreateDefault()
        {
            var data = new ExamplePlayerData
            {
                coins = startingCoins,
                level = startingLevel,
                musicVolume = defaultMusicVolume,
                sfxVolume = defaultSfxVolume,
                lastPlayed = System.DateTime.Now
            };

            if (startingItems != null)
            {
                data.inventory.AddRange(startingItems);
            }

            return data;
        }
    }
}
