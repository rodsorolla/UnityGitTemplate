namespace Sorolla.Purchasing
{
    /// <summary>
    /// Storefront product type. Mirrors UnityEngine.Purchasing.ProductType for the two
    /// kinds we ship; subscriptions are out of scope for this cut.
    /// </summary>
    public enum PurchaseProductType
    {
        Consumable,
        NonConsumable
    }
}
