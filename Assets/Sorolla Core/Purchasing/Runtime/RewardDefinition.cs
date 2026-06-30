using UnityEngine;

namespace Sorolla.Purchasing
{
    /// <summary>
    /// Base type for everything granted by a purchase. Concrete subclasses live as
    /// ScriptableObject assets and are referenced from a ProductDefinition's reward list.
    /// Game-specific rewards (LivesReward, BoosterReward) live in _Game.
    /// </summary>
    public abstract class RewardDefinition : ScriptableObject
    {
        public abstract GrantPolicy Policy { get; }
    }
}
