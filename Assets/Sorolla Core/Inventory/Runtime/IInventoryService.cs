using System;
using System.Collections.Generic;

namespace Sorolla.Inventory
{
    /// <summary>
    /// Service interface for managing game inventory.
    /// </summary>
    public interface IInventoryService
    {
        /// <summary>
        /// Gets the count of a specific item.
        /// </summary>
        int GetItemCount(string itemId);

        /// <summary>
        /// Gets the total count of all items in inventory.
        /// </summary>
        int GetTotalCount();

        /// <summary>
        /// Adds items to the inventory.
        /// </summary>
        void AddItem(string itemId, int amount = 1);

        /// <summary>
        /// Removes items from the inventory.
        /// </summary>
        void RemoveItem(string itemId, int amount = 1);

        /// <summary>
        /// Checks if the inventory contains at least one of the specified item.
        /// </summary>
        bool HasItem(string itemId);

        /// <summary>
        /// Gets all items as a read-only dictionary.
        /// </summary>
        IReadOnlyDictionary<string, int> GetAllItems();

        /// <summary>
        /// Event fired when inventory changes.
        /// </summary>
        event Action<InventoryChangedEventArgs> OnInventoryChanged;

        /// <summary>
        /// Manually saves inventory data to disk.
        /// Called automatically on pause/quit.
        /// </summary>
        void Save();
    }
}
