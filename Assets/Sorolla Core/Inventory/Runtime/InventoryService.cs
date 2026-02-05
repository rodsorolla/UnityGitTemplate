using System;
using System.Collections.Generic;
using Sorolla.PersistentData;
using UnityEngine;

namespace Sorolla.Inventory
{
    /// <summary>
    /// Generic inventory management service.
    /// Handles persistence automatically via SaveSystem.
    /// Extends SorollaManager for proper initialization order with other Sorolla services.
    /// </summary>
    public class InventoryService : SorollaManager, IInventoryService
    {
        protected const string DefaultSaveFileName = "inventory";

        protected InventoryData _data;
        protected bool _isDirty;

        /// <summary>
        /// Override in derived classes to use a different save file name.
        /// </summary>
        protected virtual string SaveFileName => DefaultSaveFileName;

        /// <inheritdoc/>
        public event Action<InventoryChangedEventArgs> OnInventoryChanged;

        protected override void Initialize()
        {
            Load();
            ServiceLocator.Instance.Register<IInventoryService>(this);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _isDirty)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            if (_isDirty)
            {
                Save();
            }
        }

        #region IInventoryService Implementation

        /// <inheritdoc/>
        public int GetItemCount(string itemId)
        {
            return _data.GetCount(itemId);
        }

        /// <inheritdoc/>
        public int GetTotalCount()
        {
            return _data.GetTotalCount();
        }

        /// <inheritdoc/>
        public virtual void AddItem(string itemId, int amount = 1)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[{GetType().Name}] AddItem amount must be positive. Got: {amount}");
                return;
            }

            var previousCount = _data.GetCount(itemId);
            var newCount = previousCount + amount;
            _data.SetCount(itemId, newCount);
            _isDirty = true;

            InvokeChanged(itemId, previousCount, newCount, InventoryChangeType.Added);
        }

        /// <inheritdoc/>
        public void RemoveItem(string itemId, int amount = 1)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[{GetType().Name}] RemoveItem amount must be positive. Got: {amount}");
                return;
            }

            var previousCount = _data.GetCount(itemId);
            if (previousCount == 0) return;

            var newCount = Mathf.Max(0, previousCount - amount);
            _data.SetCount(itemId, newCount);
            _isDirty = true;

            InvokeChanged(itemId, previousCount, newCount, InventoryChangeType.Removed);
        }

        /// <inheritdoc/>
        public bool HasItem(string itemId)
        {
            return _data.GetCount(itemId) > 0;
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, int> GetAllItems()
        {
            return _data.items;
        }

        #endregion

        #region Persistence

        /// <inheritdoc/>
        public virtual void Save()
        {
            var result = SaveSystem.Save(_data, SaveFileName);
            if (result.Success)
            {
                _isDirty = false;
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] Save failed: {result.ErrorMessage}");
            }
        }

        protected virtual void Load()
        {
            _data = SaveSystem.Load<InventoryData>(SaveFileName);
            _isDirty = false;
        }

        #endregion

        protected void InvokeChanged(string itemId, int previousCount, int newCount, InventoryChangeType changeType)
        {
            OnInventoryChanged?.Invoke(new InventoryChangedEventArgs(
                itemId,
                previousCount,
                newCount,
                changeType
            ));
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// [DEBUG] Sets an item count directly.
        /// </summary>
        public void DEBUG_SetCount(string itemId, int count)
        {
            var previousCount = _data.GetCount(itemId);
            _data.SetCount(itemId, Mathf.Max(0, count));
            _isDirty = true;

            InvokeChanged(itemId, previousCount, count, InventoryChangeType.Set);
            Debug.Log($"[{GetType().Name}] DEBUG: Set {itemId} = {count}");
        }

        /// <summary>
        /// [DEBUG] Clears all inventory.
        /// </summary>
        public void DEBUG_ClearAll()
        {
            _data = new InventoryData();
            _isDirty = true;
            Debug.Log($"[{GetType().Name}] DEBUG: Cleared all items");
        }
#endif
    }
}
