using System;
using System.Collections.Generic;
using Sorolla.PersistentData;

namespace Sorolla.Inventory
{
    /// <summary>
    /// Serializable data model for inventory items.
    /// </summary>
    [Serializable]
    public class InventoryData : ISaveData
    {
        public int Version => 1;

        /// <summary>
        /// Dictionary of item ID to count.
        /// </summary>
        public Dictionary<string, int> items = new();

        /// <summary>
        /// Gets the count for an item, or 0 if not found.
        /// </summary>
        public int GetCount(string itemId)
        {
            return items.TryGetValue(itemId, out var count) ? count : 0;
        }

        /// <summary>
        /// Sets the count for an item.
        /// </summary>
        public void SetCount(string itemId, int count)
        {
            if (count <= 0)
            {
                items.Remove(itemId);
            }
            else
            {
                items[itemId] = count;
            }
        }

        /// <summary>
        /// Gets the total count of all items.
        /// </summary>
        public int GetTotalCount()
        {
            int total = 0;
            foreach (var count in items.Values)
            {
                total += count;
            }
            return total;
        }
    }
}
