using System.Collections.Generic;
using UnityEngine;

namespace Sorolla.Purchasing
{
    /// <summary>
    /// Designer-authored product. Holds the storefront ID(s), product type, the list of
    /// RewardDefinitions to grant, the shop section to render in, and the prefab the
    /// ShopUI should instantiate to display this product.
    /// </summary>
    [CreateAssetMenu(menuName = "Sorolla/Purchasing/Product", fileName = "ProductDef")]
    public class ProductDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _productId;
        [SerializeField] private string _appleProductId;     // optional override
        [SerializeField] private string _googleProductId;    // optional override
        [SerializeField] private PurchaseProductType _type;

        [Header("Rewards")]
        [SerializeField] private List<RewardDefinition> _rewards = new();

        [Header("Display")]
        [SerializeField] private ShopSection _shopSection;
        [SerializeField] private GameObject _cardPrefab;
        [SerializeField] private string _displayTitleKey;
        [SerializeField] private Sprite _icon;
        [SerializeField] private string _badgeText;

        [Header("Availability")]
        [Tooltip("Hide this product after a single purchase. The 'bought' flag lives in LOCAL save data only — a reinstall wipes it, so the player can buy it again. Use for one-time consumable offers (e.g. a starter pack). This is NOT a non-consumable: nothing is restored from the store.")]
        [SerializeField] private bool _oncePerInstall;

        public string ProductId => _productId;
        public PurchaseProductType Type => _type;
        public IReadOnlyList<RewardDefinition> Rewards => _rewards;
        public ShopSection ShopSection => _shopSection;
        public GameObject CardPrefab => _cardPrefab;
        public string DisplayTitleKey => _displayTitleKey;
        public Sprite Icon => _icon;
        public string BadgeText => _badgeText;
        public bool OncePerInstall => _oncePerInstall;

        /// <summary>
        /// Local-save key marking this product as already bought once on this install.
        /// Used by OncePerInstall products only. Wiped on reinstall (local save), never
        /// restored from the store.
        /// </summary>
        public static string LocalPurchaseKey(string productId) => "purchased_" + productId;

        /// <summary>
        /// Returns the platform-specific store ID if set, otherwise the canonical _productId.
        /// </summary>
        public string GetStoreSpecificId(RuntimePlatform platform)
        {
            if (platform == RuntimePlatform.IPhonePlayer || platform == RuntimePlatform.OSXPlayer)
                return string.IsNullOrEmpty(_appleProductId) ? _productId : _appleProductId;
            if (platform == RuntimePlatform.Android)
                return string.IsNullOrEmpty(_googleProductId) ? _productId : _googleProductId;
            return _productId;
        }
    }
}
