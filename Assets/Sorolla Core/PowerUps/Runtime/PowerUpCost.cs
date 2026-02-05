using System;
using UnityEngine;

namespace Sorolla.PowerUps
{
    /// <summary>
    /// Defines the cost to purchase a power-up.
    /// </summary>
    [Serializable]
    public struct PowerUpCost
    {
        [Tooltip("Currency ID (e.g., 'coins', 'gems')")]
        public string currencyId;

        [Tooltip("Amount of currency required")]
        [Min(0)]
        public int amount;

        public PowerUpCost(string currencyId, int amount)
        {
            this.currencyId = currencyId;
            this.amount = amount;
        }

        /// <summary>
        /// Whether this cost is valid (has currency and positive amount).
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(currencyId) && amount > 0;
    }
}
