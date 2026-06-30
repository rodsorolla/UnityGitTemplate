namespace Sorolla.Purchasing
{
    /// <summary>
    /// Passed to every reward handler so handlers can react to restore vs first-time
    /// grants (e.g. suppress UI feedback / coin-gain VFX on restore).
    /// </summary>
    public readonly struct PurchaseRewardContext
    {
        public ProductDefinition Product { get; }
        public bool IsRestore { get; }
        public bool IsFirstTime { get; }

        public PurchaseRewardContext(ProductDefinition product, bool isRestore, bool isFirstTime)
        {
            Product = product;
            IsRestore = isRestore;
            IsFirstTime = isFirstTime;
        }
    }
}
