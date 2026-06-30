using System;
using System.Collections.Generic;

namespace Sorolla.Events
{
    /// <summary>
    /// Generic, string-keyed reward payload. Catalog data describes any reward
    /// (coins, gems, booster, skin shards, skin) without per-game C# types.
    /// </summary>
    [Serializable]
    public sealed class EventReward
    {
        /// <summary>e.g. "coins", "gems", "booster", "skin_shards", "skin".</summary>
        public string ItemType;

        /// <summary>e.g. "magnet", "golden_serpent". Nullable for fungible types like coins.</summary>
        public string ItemId;

        /// <summary>Quantity. For "skin" type this is typically 1.</summary>
        public int Amount;

        /// <summary>Forward-compat payload for future fields without schema bumps.</summary>
        public Dictionary<string, string> Extras = new Dictionary<string, string>();
    }
}
