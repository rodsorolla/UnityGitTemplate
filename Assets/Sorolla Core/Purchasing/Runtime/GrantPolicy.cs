namespace Sorolla.Purchasing
{
    /// <summary>
    /// Controls whether a reward fires every time the purchase pipeline runs (including
    /// restore re-fires) or only on first-time grants. EveryPurchase is for consumables
    /// inside non-consumable bundles — they need to re-fire on every fresh purchase but
    /// NOT on restore, so the service skips them when IsFirstTime is false.
    /// OncePerProduct is for entitlements: handlers must be idempotent because restore
    /// will re-invoke them.
    /// </summary>
    public enum GrantPolicy
    {
        EveryPurchase,
        OncePerProduct
    }
}
