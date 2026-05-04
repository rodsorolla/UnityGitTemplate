using System;
using System.Collections.Generic;
using Sorolla.PersistentData;

namespace Sorolla.Currency
{
    /// <summary>
    /// Serializable data model for currency balances.
    /// </summary>
    [Serializable]
    public class CurrencyData : ISaveData
    {
        public int Version => 1;

        /// <summary>
        /// Dictionary of currency ID to balance.
        /// </summary>
        public Dictionary<string, int> balances = new()
        {
            [CurrencyIds.Coins] = 1000,
            [CurrencyIds.Gems] = 0,
            [CurrencyIds.Energy] = 100
        };

        /// <summary>
        /// Gets the balance for a currency, or 0 if not found.
        /// </summary>
        public int GetBalance(string currencyId)
        {
            return balances.TryGetValue(currencyId, out var val) ? val : 0;
        }

        /// <summary>
        /// Sets the balance for a currency.
        /// </summary>
        public void SetBalance(string currencyId, int amount)
        {
            balances[currencyId] = amount;
        }
    }
}
