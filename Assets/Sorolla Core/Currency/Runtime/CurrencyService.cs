using System;
using System.Collections.Generic;
using Sorolla;
using Sorolla.PersistentData;
using UnityEngine;

namespace Sorolla.Currency
{
    /// <summary>
    /// Self-contained currency management service.
    /// Handles persistence automatically via SaveSystem.
    /// Extends SorollaManager for proper initialization order with other Sorolla services.
    /// </summary>
    public class CurrencyService : SorollaManager, ICurrencyService
    {
        private const string SaveFileName = "currency";

        private CurrencyData _data;
        private bool _isDirty;

        /// <inheritdoc/>
        public event Action<CurrencyChangedEventArgs> OnCurrencyChanged;

        protected override void Initialize()
        {
            Load();
            ServiceLocator.Instance.Register<ICurrencyService>(this);
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

        /// <inheritdoc/>
        public int GetBalance(string currencyId)
        {
            return _data.GetBalance(currencyId);
        }

        /// <inheritdoc/>
        public bool CanAfford(string currencyId, int amount)
        {
            return _data.GetBalance(currencyId) >= amount;
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, int> GetAllBalances()
        {
            return _data.balances;
        }

        /// <inheritdoc/>
        public void Add(string currencyId, int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyService] Add amount must be positive. Got: {amount}");
                return;
            }

            var previousBalance = _data.GetBalance(currencyId);
            var newBalance = previousBalance + amount;
            _data.SetBalance(currencyId, newBalance);
            _isDirty = true;

            InvokeChanged(currencyId, previousBalance, newBalance, CurrencyChangeType.Add);
        }

        /// <inheritdoc/>
        public bool TrySpend(string currencyId, int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyService] Spend amount must be positive. Got: {amount}");
                return false;
            }

            var previousBalance = _data.GetBalance(currencyId);
            if (previousBalance < amount)
            {
                return false;
            }

            var newBalance = previousBalance - amount;
            _data.SetBalance(currencyId, newBalance);
            _isDirty = true;

            InvokeChanged(currencyId, previousBalance, newBalance, CurrencyChangeType.Spend);
            return true;
        }

        /// <inheritdoc/>
        public void Set(string currencyId, int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[CurrencyService] Set amount cannot be negative. Got: {amount}");
                return;
            }

            var previousBalance = _data.GetBalance(currencyId);
            if (previousBalance == amount) return;

            _data.SetBalance(currencyId, amount);
            _isDirty = true;

            InvokeChanged(currencyId, previousBalance, amount, CurrencyChangeType.Set);
        }

        /// <inheritdoc/>
        public void Save()
        {
            var result = SaveSystem.Save(_data, SaveFileName);
            if (result.Success)
            {
                _isDirty = false;
            }
            else
            {
                Debug.LogError($"[CurrencyService] Save failed: {result.ErrorMessage}");
            }
        }

        /// <inheritdoc/>
        public void Load()
        {
            _data = SaveSystem.Load<CurrencyData>(SaveFileName);
            _isDirty = false;
        }

        private void InvokeChanged(string currencyId, int previousBalance, int newBalance, CurrencyChangeType changeType)
        {
            OnCurrencyChanged?.Invoke(new CurrencyChangedEventArgs(
                currencyId,
                previousBalance,
                newBalance,
                changeType
            ));
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// [DEBUG] Sets a currency balance directly.
        /// </summary>
        public void DEBUG_SetBalance(string currencyId, int amount)
        {
            var previousBalance = _data.GetBalance(currencyId);
            _data.SetBalance(currencyId, Mathf.Max(0, amount));
            _isDirty = true;

            InvokeChanged(currencyId, previousBalance, amount, CurrencyChangeType.Set);
            Debug.Log($"[CurrencyService] DEBUG: Set {currencyId} = {amount}");
        }

        /// <summary>
        /// [DEBUG] Resets all currencies to their default values.
        /// </summary>
        public void DEBUG_ResetAll()
        {
            var oldBalances = new Dictionary<string, int>(_data.balances);
            _data = new CurrencyData();
            _isDirty = true;

            foreach (var kvp in oldBalances)
            {
                var newBalance = _data.GetBalance(kvp.Key);
                if (kvp.Value != newBalance)
                {
                    InvokeChanged(kvp.Key, kvp.Value, newBalance, CurrencyChangeType.Reset);
                }
            }

            Debug.Log("[CurrencyService] DEBUG: Reset all currencies to defaults");
        }

        /// <summary>
        /// [DEBUG] Adds an amount to all existing currencies.
        /// </summary>
        public void DEBUG_AddToAll(int amount)
        {
            foreach (var currencyId in new List<string>(_data.balances.Keys))
            {
                Add(currencyId, amount);
            }
            Debug.Log($"[CurrencyService] DEBUG: Added {amount} to all currencies");
        }

        /// <summary>
        /// [DEBUG] Logs all currency balances.
        /// </summary>
        public void DEBUG_ListAll()
        {
            Debug.Log("[CurrencyService] Current balances:");
            foreach (var kvp in _data.balances)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }
        }
#endif
    }
}
