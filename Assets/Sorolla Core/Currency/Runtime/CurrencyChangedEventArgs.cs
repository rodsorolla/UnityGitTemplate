namespace Sorolla.Currency
{
    /// <summary>
    /// Event arguments for currency change events.
    /// </summary>
    public class CurrencyChangedEventArgs
    {
        /// <summary>The currency that changed.</summary>
        public string CurrencyId { get; }

        /// <summary>The balance before the change.</summary>
        public int PreviousBalance { get; }

        /// <summary>The balance after the change.</summary>
        public int NewBalance { get; }

        /// <summary>The amount of change (positive or negative).</summary>
        public int Delta => NewBalance - PreviousBalance;

        /// <summary>The type of change that occurred.</summary>
        public CurrencyChangeType ChangeType { get; }

        public CurrencyChangedEventArgs(
            string currencyId,
            int previousBalance,
            int newBalance,
            CurrencyChangeType changeType)
        {
            CurrencyId = currencyId;
            PreviousBalance = previousBalance;
            NewBalance = newBalance;
            ChangeType = changeType;
        }
    }
}
