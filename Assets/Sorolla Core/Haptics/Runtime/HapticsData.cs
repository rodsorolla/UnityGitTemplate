using System;
using Sorolla.PersistentData;

namespace Sorolla
{
    /// <summary>
    /// Serializable data model for haptics settings.
    /// </summary>
    [Serializable]
    public class HapticsData : ISaveData
    {
        public int Version => 1;

        /// <summary>
        /// Whether haptic feedback is enabled.
        /// </summary>
        public bool isEnabled = true;
    }
}
