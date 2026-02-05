using UnityEngine;

namespace Sorolla.LevelFlow
{
    /// <summary>
    /// Configuration for a world/chapter containing multiple levels.
    /// Optional - only needed if using the world system.
    /// </summary>
    [CreateAssetMenu(fileName = "World", menuName = "Sorolla/Level Flow/World Config")]
    public class WorldConfig : ScriptableObject
    {
        [Tooltip("Display name for this world")]
        public string worldName;

        [Tooltip("Number of levels in this world")]
        [Min(1)]
        public int levelCount = 20;

        [Tooltip("Optional icon for world selection UI")]
        public Sprite icon;

        [Tooltip("Optional description for world selection UI")]
        [TextArea(2, 4)]
        public string description;
    }
}
