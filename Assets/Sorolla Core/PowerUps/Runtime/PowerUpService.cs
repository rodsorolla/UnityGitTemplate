using System;
using System.Collections.Generic;
using Sorolla.Currency;
using Sorolla.LevelFlow;
using Sorolla.PersistentData;
using UnityEngine;

namespace Sorolla.PowerUps
{
    /// <summary>
    /// Service for managing power-up inventory, unlocks, and purchases.
    /// Handles persistence automatically via SaveSystem.
    /// Extends SorollaManager for proper initialization order with other Sorolla services.
    /// </summary>
    public class PowerUpService : SorollaManager, IPowerUpService
    {
        private const string SaveFileName = "powerups";

        [Header("Configuration")]
        [SerializeField] private PowerUpRegistry _registry;

        private PowerUpData _data;
        private Dictionary<PowerUpId, PowerUpDefinitionBase> _definitionCache;
        private bool _isDirty;

        // Events
        public event Action<PowerUpQuantityChangedEventArgs> OnQuantityChanged;
        public event Action<PowerUpUnlockedEventArgs> OnPowerUpUnlocked;
        public event Action<PowerUpId> OnPowerUpUsed;

        protected override void Initialize()
        {
            BuildDefinitionCache();
            Load();
            InitializeUnlockedPowerUps();

            // Register with ServiceLocator
            ServiceLocator.Instance.Register<IPowerUpService>(this);

            // Subscribe to level start for unlock checks — unlocks should appear
            // when the new level begins, not at the end of the previous one.
            var levelFlow = ServiceLocator.Instance.TryResolve<ILevelFlowManager>();
            if (levelFlow != null)
            {
                levelFlow.OnLevelStarted += OnLevelStarted;
                CheckUnlocks(levelFlow.HighestLevelReached);
            }
        }

        private void OnDestroy()
        {
            var levelFlow = ServiceLocator.Instance.TryResolve<ILevelFlowManager>();
            if (levelFlow != null)
            {
                levelFlow.OnLevelStarted -= OnLevelStarted;
            }
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

        #region Initialization

        private void BuildDefinitionCache()
        {
            _definitionCache = new Dictionary<PowerUpId, PowerUpDefinitionBase>();

            if (_registry == null)
            {
                Debug.LogWarning("[PowerUpService] No registry assigned");
                return;
            }

            foreach (var definition in _registry.PowerUps)
            {
                if (definition != null)
                {
                    _definitionCache[definition.PowerUpId] = definition;
                }
            }
        }

        private void InitializeUnlockedPowerUps()
        {
            // Initialize power-ups that don't require unlocking (unlock level = 0)
            foreach (var kvp in _definitionCache)
            {
                var definition = kvp.Value;
                if (!definition.RequiresUnlock)
                {
                    var key = kvp.Key.ToKey();
                    var state = _data.GetState(key);
                    if (!state.isUnlocked)
                    {
                        state.isUnlocked = true;
                        state.quantity = definition.InitialQuantity;
                        _isDirty = true;
                    }
                }
            }
        }

        #endregion

        #region IPowerUpService Implementation

        public int GetQuantity(PowerUpId powerUpId)
        {
            return _data.GetQuantity(powerUpId.ToKey());
        }

        public bool IsUnlocked(PowerUpId powerUpId)
        {
            // Check if already unlocked in save data
            if (_data.IsUnlocked(powerUpId.ToKey()))
            {
                return true;
            }

            // Check if definition doesn't require unlock
            if (_definitionCache.TryGetValue(powerUpId, out var definition))
            {
                return !definition.RequiresUnlock;
            }

            return false;
        }

        public bool CanUse(PowerUpId powerUpId)
        {
            if (!IsUnlocked(powerUpId)) return false;

            // Can use if first use is free OR has quantity
            return IsFirstUseFree(powerUpId) || GetQuantity(powerUpId) > 0;
        }

        public bool IsFirstUseFree(PowerUpId powerUpId)
        {
            return IsUnlocked(powerUpId) && !_data.HasUsedFirstFree(powerUpId.ToKey());
        }

        public bool TryUse(PowerUpId powerUpId)
        {
            if (!CanUse(powerUpId))
            {
                return false;
            }

            var key = powerUpId.ToKey();

            // Check if this is the first free use
            bool isFirstFree = IsFirstUseFree(powerUpId);

            if (isFirstFree)
            {
                // Mark first free use as consumed, don't decrement quantity
                _data.SetFirstFreeUsed(key);
                _isDirty = true;

                // Fire quantity changed event to update UI (quantity stays the same but "free" label should hide)
                var quantity = _data.GetQuantity(key);
                OnQuantityChanged?.Invoke(new PowerUpQuantityChangedEventArgs(powerUpId, quantity, quantity));
                OnPowerUpUsed?.Invoke(powerUpId);
            }
            else
            {
                // Normal use - decrement quantity
                var previousQuantity = _data.GetQuantity(key);
                var newQuantity = previousQuantity - 1;
                _data.SetQuantity(key, newQuantity);
                _isDirty = true;

                OnQuantityChanged?.Invoke(new PowerUpQuantityChangedEventArgs(powerUpId, previousQuantity, newQuantity));
                OnPowerUpUsed?.Invoke(powerUpId);
            }

            return true;
        }

        public void AddQuantity(PowerUpId powerUpId, int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[PowerUpService] AddQuantity amount must be positive. Got: {amount}");
                return;
            }

            if (!IsUnlocked(powerUpId))
            {
                Debug.LogWarning($"[PowerUpService] Cannot add quantity to locked power-up: {powerUpId}");
                return;
            }

            var key = powerUpId.ToKey();
            var previousQuantity = _data.GetQuantity(key);
            var newQuantity = previousQuantity + amount;

            // Respect max quantity
            if (_definitionCache.TryGetValue(powerUpId, out var definition) && definition.MaxQuantity >= 0)
            {
                newQuantity = Mathf.Min(newQuantity, definition.MaxQuantity);
            }

            if (newQuantity == previousQuantity) return;

            _data.SetQuantity(key, newQuantity);
            _isDirty = true;

            OnQuantityChanged?.Invoke(new PowerUpQuantityChangedEventArgs(powerUpId, previousQuantity, newQuantity));
        }

        public bool TryPurchase(PowerUpId powerUpId)
        {
            if (!CanAffordPurchase(powerUpId))
            {
                return false;
            }

            if (!_definitionCache.TryGetValue(powerUpId, out var definition))
            {
                return false;
            }

            var currencyService = ServiceLocator.Instance.TryResolve<ICurrencyService>();
            if (currencyService == null)
            {
                Debug.LogWarning("[PowerUpService] CurrencyService not available for purchase");
                return false;
            }

            var cost = definition.Cost;
            if (!currencyService.TrySpend(cost.currencyId, cost.amount))
            {
                return false;
            }

            AddQuantity(powerUpId, 1);
            return true;
        }

        public bool CanAffordPurchase(PowerUpId powerUpId)
        {
            if (!IsUnlocked(powerUpId))
            {
                return false;
            }

            if (!_definitionCache.TryGetValue(powerUpId, out var definition))
            {
                return false;
            }

            if (!definition.HasCost)
            {
                return false; // Can't purchase free power-ups
            }

            var currencyService = ServiceLocator.Instance.TryResolve<ICurrencyService>();
            if (currencyService == null)
            {
                return false;
            }

            return currencyService.CanAfford(definition.Cost.currencyId, definition.Cost.amount);
        }

        public PowerUpDefinitionBase GetDefinition(PowerUpId powerUpId)
        {
            _definitionCache.TryGetValue(powerUpId, out var definition);
            return definition;
        }

        public bool HasSeenUnlockNotification(PowerUpId powerUpId)
        {
            return _data.HasSeenUnlockNotification(powerUpId.ToKey());
        }

        public void MarkUnlockNotificationSeen(PowerUpId powerUpId)
        {
            var key = powerUpId.ToKey();
            if (!_data.HasSeenUnlockNotification(key))
            {
                _data.SetUnlockNotificationSeen(key);
                _isDirty = true;
            }
        }

        public bool HasSeenUnlockCelebration(PowerUpId powerUpId)
        {
            return _data.HasSeenUnlockCelebration(powerUpId.ToKey());
        }

        public void MarkUnlockCelebrationSeen(PowerUpId powerUpId)
        {
            var key = powerUpId.ToKey();
            if (!_data.HasSeenUnlockCelebration(key))
            {
                _data.SetUnlockCelebrationSeen(key);
                _isDirty = true;
            }
        }

        public PowerUpDefinitionBase GetNextPendingCelebration()
        {
            if (_registry == null) return null;

            var levelFlow = ServiceLocator.Instance.TryResolve<ILevelFlowManager>();
            if (levelFlow == null) return null;

            var highest = levelFlow.HighestLevelReached;

            foreach (var definition in _registry.GetLockablePowerUps())
            {
                if (highest >= definition.UnlockLevel
                    && !_data.HasSeenUnlockCelebration(definition.PowerUpId.ToKey()))
                {
                    return definition;
                }
            }

            return null;
        }

        #endregion

        #region Persistence

        public void Save()
        {
            var result = SaveSystem.Save(_data, SaveFileName);
            if (result.Success)
            {
                _isDirty = false;
            }
            else
            {
                Debug.LogError($"[PowerUpService] Save failed: {result.ErrorMessage}");
            }
        }

        public void Load()
        {
            _data = SaveSystem.Load<PowerUpData>(SaveFileName);
            _isDirty = false;
        }

        #endregion

        #region Unlock System

        public void CheckUnlocks(int currentHighestLevel)
        {
            if (_registry == null) return;

            foreach (var definition in _registry.GetLockablePowerUps())
            {
                var key = definition.PowerUpId.ToKey();

                // Skip if already unlocked
                if (_data.IsUnlocked(key))
                {
                    continue;
                }

                // Check if should unlock
                if (currentHighestLevel >= definition.UnlockLevel)
                {
                    UnlockPowerUp(definition);
                }
            }
        }

        private void UnlockPowerUp(PowerUpDefinitionBase definition)
        {
            var key = definition.PowerUpId.ToKey();
            var state = _data.GetState(key);
            state.isUnlocked = true;
            state.quantity = definition.InitialQuantity;
            _isDirty = true;

            // Save immediately to ensure unlock persists across scene transitions
            Save();

            OnPowerUpUnlocked?.Invoke(new PowerUpUnlockedEventArgs(
                definition.PowerUpId,
                definition,
                definition.UnlockLevel
            ));
        }

        private void OnLevelStarted(int levelIndex)
        {
            var levelFlow = ServiceLocator.Instance.TryResolve<ILevelFlowManager>();
            if (levelFlow != null)
            {
                CheckUnlocks(levelFlow.HighestLevelReached);
            }
        }

        #endregion

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// [DEBUG] Sets a power-up quantity directly.
        /// </summary>
        public void DEBUG_SetQuantity(PowerUpId powerUpId, int quantity)
        {
            var key = powerUpId.ToKey();
            var previousQuantity = _data.GetQuantity(key);
            _data.SetQuantity(key, Mathf.Max(0, quantity));
            _isDirty = true;

            OnQuantityChanged?.Invoke(new PowerUpQuantityChangedEventArgs(powerUpId, previousQuantity, quantity));
            Debug.Log($"[PowerUpService] DEBUG: Set {powerUpId} = {quantity}");
        }

        /// <summary>
        /// [DEBUG] Unlocks a power-up regardless of level.
        /// </summary>
        public void DEBUG_Unlock(PowerUpId powerUpId)
        {
            if (_definitionCache.TryGetValue(powerUpId, out var definition))
            {
                if (!_data.IsUnlocked(powerUpId.ToKey()))
                {
                    UnlockPowerUp(definition);
                }
            }
        }

        /// <summary>
        /// [DEBUG] Resets all power-up data.
        /// </summary>
        public void DEBUG_ResetAll()
        {
            _data = new PowerUpData();
            InitializeUnlockedPowerUps();
            _isDirty = true;
            Debug.Log("[PowerUpService] DEBUG: Reset all power-ups");
        }
#endif
    }
}
