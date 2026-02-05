namespace Sorolla.Currency
{
    /// <summary>
    /// The type of currency change that occurred.
    /// </summary>
    public enum CurrencyChangeType
    {
        /// <summary>Currency was added to the balance.</summary>
        Add,

        /// <summary>Currency was spent from the balance.</summary>
        Spend,

        /// <summary>Currency was set to a specific value.</summary>
        Set,

        /// <summary>Currency was reset (typically to default or zero).</summary>
        Reset
    }
}
