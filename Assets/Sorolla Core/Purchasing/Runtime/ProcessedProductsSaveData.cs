using System;
using System.Collections.Generic;
using Sorolla.PersistentData;

namespace Sorolla.Purchasing
{
    /// <summary>
    /// Persists the list of NonConsumable productIds that have already been processed,
    /// gating EveryPurchase rewards inside non-consumable bundles from re-firing on
    /// restore. Consumables are NOT tracked here — the store-side receipt completion
    /// is the source of truth for them.
    /// </summary>
    [Serializable]
    public class ProcessedProductsSaveData : ISaveData
    {
        public int Version => 1;
        public List<string> Processed = new();
    }
}
