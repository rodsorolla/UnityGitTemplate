using UnityEngine;

namespace Sorolla.UI
{
    /// <summary>
    /// Resolves a sprite from a typed (itemType, itemId) pair. Data-driven LiveOps
    /// surfaces (TreasureHunt, future Battle Pass / Daily Rewards / Tournaments)
    /// call this so they don't each maintain their own inline sprite map.
    ///
    /// Existing UI that holds direct sprite references (BoosterData.icon,
    /// ProductDef._icon, scattered _coinSprite fields) is intentionally NOT
    /// migrated — it works and changing it adds risk. New data-driven systems
    /// use the resolver; legacy refs stay until touched for other reasons.
    /// </summary>
    public interface IIconResolver
    {
        /// <summary>
        /// Returns the sprite for the given typed item. <paramref name="itemId"/>
        /// is optional and only meaningful when an ItemType has multiple variants
        /// (e.g. <c>booster</c>+<c>magnet</c> vs <c>booster</c>+<c>freeze</c>).
        /// Returns <c>null</c> if no mapping exists.
        /// </summary>
        Sprite Resolve(string itemType, string itemId);
    }
}
