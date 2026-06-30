namespace Sorolla.Purchasing
{
    /// <summary>
    /// Shop layout section a product is rendered into. ShopUI uses this to pick the
    /// section RectTransform when spawning the product's card prefab.
    /// </summary>
    // Explicit values: ShopSection is serialized as an int in ProductDefinition assets.
    // Never reorder or reuse a value — append new sections with the next free number.
    public enum ShopSection
    {
        Bundle = 0,
        NoAds = 1,
        CoinPacks = 2,
        StarterPack = 3
    }
}
