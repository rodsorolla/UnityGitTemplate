using System;
using System.Collections.Generic;

namespace Sorolla.Currency
{
    /// <summary>
    /// Service interface for managing game currencies.
    /// </summary>
    public interface ICurrencyService
    {
        /// <summary>
        /// Gets the current balance of a currency.
        /// </summary>
        /// <param name="currencyId">The currency identifier.</param>
        /// <returns>The current balance, or 0 if the currency doesn't exist.</returns>
        int GetBalance(string currencyId);

        /// <summary>
        /// Checks if the player can afford a specific amount.
        /// </summary>
        /// <param name="currencyId">The currency identifier.</param>
        /// <param name="amount">The amount to check.</param>
        /// <returns>True if balance >= amount.</returns>
        bool CanAfford(string currencyId, int amount);

        /// <summary>
        /// Gets all currency balances as a read-only dictionary.
        /// </summary>
        IReadOnlyDictionary<string, int> GetAllBalances();

        /// <summary>
        /// Adds an amount to a currency balance.
        /// </summary>
        /// <param name="currencyId">The currency identifier.</param>
        /// <param name="amount">The amount to add (must be positive).</param>
        void Add(string currencyId, int amount);

        /// <summary>
        /// Attempts to spend an amount from a currency balance.
        /// </summary>
        /// <param name="currencyId">The currency identifier.</param>
        /// <param name="amount">The amount to spend.</param>
        /// <returns>True if successful, false if insufficient balance.</returns>
        bool TrySpend(string currencyId, int amount);

        /// <summary>
        /// Sets a currency balance to a specific value.
        /// </summary>
        /// <param name="currencyId">The currency identifier.</param>
        /// <param name="amount">The new balance value.</param>
        void Set(string currencyId, int amount);

        /// <summary>
        /// Event fired when any currency balance changes.
        /// </summary>
        event Action<CurrencyChangedEventArgs> OnCurrencyChanged;

        /// <summary>
        /// Manually saves currency data to disk.
        /// Called automatically on pause/quit.
        /// </summary>
        void Save();

        /// <summary>
        /// Manually loads currency data from disk.
        /// Called automatically on startup.
        /// </summary>
        void Load();
    }
}
