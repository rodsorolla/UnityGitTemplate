namespace Sorolla.Inventory
{
    /// <summary>
    /// Event arguments for inventory change events.
    /// </summary>
    public readonly struct InventoryChangedEventArgs
    {
        /// <summary>The item that changed.</summary>
        public string ItemId { get; }

        /// <summary>The count before the change.</summary>
        public int PreviousCount { get; }

        /// <summary>The count after the change.</summary>
        public int NewCount { get; }

        /// <summary>The amount of change (positive or negative).</summary>
        public int Delta => NewCount - PreviousCount;

        /// <summary>The type of change that occurred.</summary>
        public InventoryChangeType ChangeType { get; }

        public InventoryChangedEventArgs(
            string itemId,
            int previousCount,
            int newCount,
            InventoryChangeType changeType)
        {
            ItemId = itemId;
            PreviousCount = previousCount;
            NewCount = newCount;
            ChangeType = changeType;
        }
    }
}
