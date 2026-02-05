using Sorolla;
using UnityEngine;

namespace Sorolla.Currency.Samples
{
    /// <summary>
    /// Example showing how to use the CurrencyService.
    /// </summary>
    public class CurrencyUsageExample : MonoBehaviour
    {
        private ICurrencyService _currency;

        private void Start()
        {
            // Get the currency service from ServiceLocator
            _currency = ServiceLocator.Instance.Resolve<ICurrencyService>();

            // Subscribe to currency changes (useful for UI updates)
            _currency.OnCurrencyChanged += OnCurrencyChanged;

            // Log initial balances
            Debug.Log($"Starting coins: {_currency.GetBalance(CurrencyIds.Coins)}");
            Debug.Log($"Starting gems: {_currency.GetBalance(CurrencyIds.Gems)}");
            Debug.Log($"Starting energy: {_currency.GetBalance(CurrencyIds.Energy)}");
        }

        private void OnDestroy()
        {
            if (_currency != null)
                _currency.OnCurrencyChanged -= OnCurrencyChanged;
        }

        private void OnCurrencyChanged(CurrencyChangedEventArgs args)
        {
            Debug.Log($"Currency changed: {args.CurrencyId} " +
                      $"{args.PreviousBalance} → {args.NewBalance} " +
                      $"(Delta: {args.Delta}, Type: {args.ChangeType})");
        }

        // Example: Call this when player collects coins
        public void CollectCoins(int amount)
        {
            _currency.Add(CurrencyIds.Coins, amount);
        }

        // Example: Call this when player tries to purchase something
        public bool TryPurchase(int coinCost)
        {
            if (!_currency.CanAfford(CurrencyIds.Coins, coinCost))
            {
                Debug.Log("Not enough coins!");
                return false;
            }

            _currency.TrySpend(CurrencyIds.Coins, coinCost);
            Debug.Log($"Purchase successful! Spent {coinCost} coins.");
            return true;
        }

        // Example: Call this for premium purchases
        public bool TryPremiumPurchase(int gemCost)
        {
            if (!_currency.CanAfford(CurrencyIds.Gems, gemCost))
            {
                Debug.Log("Not enough gems!");
                return false;
            }

            _currency.TrySpend(CurrencyIds.Gems, gemCost);
            Debug.Log($"Premium purchase successful! Spent {gemCost} gems.");
            return true;
        }

        // Example: Energy consumption for playing a level
        public bool TryConsumeEnergy(int cost = 10)
        {
            if (!_currency.CanAfford(CurrencyIds.Energy, cost))
            {
                Debug.Log("Not enough energy! Wait for it to recharge.");
                return false;
            }

            _currency.TrySpend(CurrencyIds.Energy, cost);
            return true;
        }
    }
}
