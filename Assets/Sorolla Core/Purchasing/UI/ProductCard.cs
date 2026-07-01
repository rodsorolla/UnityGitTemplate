using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Purchasing
{
    /// <summary>
    /// Single binding component for every shop card variant. Each card prefab
    /// (CoinPackCard, NoAdsCard, BundleCard, …) has this component with a different
    /// subset of the optional fields wired up. Agnostic: no ad-gating, no game reward types.
    /// </summary>
    public class ProductCard : MonoBehaviour
    {
        [Header("Optional refs (set those used by this card variant)")]
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _priceLabel;
        [SerializeField] private TextMeshProUGUI _coinsLabel;
        [SerializeField] private TextMeshProUGUI _badgeLabel;
        [SerializeField] private GameObject _badgeRoot;
        [SerializeField] private Button _buyButton;
        [SerializeField] private GameObject _ownedOverlay;
        [SerializeField] private string _loadingPriceText = "...";

        private ProductDefinition _product;
        private PurchasingService _purchasing;
        private EntitlementService _entitlements;
        private IHapticsService _haptics;

        public void Bind(ProductDefinition product, PurchasingService purchasing, EntitlementService entitlements)
        {
            _product = product;
            _purchasing = purchasing;
            _entitlements = entitlements;

            if (_icon != null && _product.Icon != null) _icon.sprite = _product.Icon;
            if (_badgeLabel != null) _badgeLabel.text = _product.BadgeText ?? string.Empty;
            if (_badgeRoot != null) _badgeRoot.SetActive(!string.IsNullOrEmpty(_product.BadgeText));
            if (_coinsLabel != null) _coinsLabel.text = ComputeCoinsLabel(_product);

            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveAllListeners();
                _buyButton.onClick.AddListener(OnBuyClicked);
            }

            Refresh();
        }

        public void Refresh()
        {
            if (_product == null) return;

            // Redundant if it grants an already-owned entitlement (e.g. anything with a
            // NoAds reward, once NoAds is owned) — hide the whole card.
            if (IsRedundant()) { gameObject.SetActive(false); return; }

            // One-time-per-install offers hide once bought (local flag, reinstall-wiped).
            if (_product.OncePerInstall && OncePerInstallStore.IsPurchased(_product.ProductId))
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);

            bool storeReady = _purchasing != null && _purchasing.IsStoreReady;
            bool owned = IsOwned();

            if (_priceLabel != null)
            {
                string price = _purchasing?.GetLocalizedPrice(_product.ProductId);
                _priceLabel.text = string.IsNullOrEmpty(price) ? _loadingPriceText : price;
            }
            if (_buyButton != null) _buyButton.interactable = storeReady && !owned;
            if (_ownedOverlay != null) _ownedOverlay.SetActive(owned);
        }

        public bool IsOwned()
        {
            if (_product == null || _entitlements == null) return false;
            bool hasEntitlement = false;
            foreach (var r in _product.Rewards)
            {
                if (r is EntitlementReward er)
                {
                    hasEntitlement = true;
                    if (!_entitlements.Has(er.Key)) return false;
                }
            }
            return hasEntitlement;
        }

        public bool IsRedundant() => IsProductRedundant(_product, _entitlements);

        public static bool IsProductRedundant(ProductDefinition product, EntitlementService entitlements)
        {
            if (product == null || entitlements == null) return false;
            foreach (var r in product.Rewards)
                if (r is EntitlementReward er && entitlements.Has(er.Key))
                    return true;
            return false;
        }

        private static string ComputeCoinsLabel(ProductDefinition product)
        {
            int total = 0;
            foreach (var r in product.Rewards)
                if (r is CoinReward c) total += c.Amount;
            return total > 0 ? total.ToString("N0") : string.Empty;
        }

        private void OnBuyClicked()
        {
            _haptics ??= ServiceLocator.Instance?.TryResolve<IHapticsService>();
            _haptics?.PlayImpact(HapticsIntensity.Light);

            if (_product == null || _purchasing == null) return;
            _purchasing.InitiatePurchase(_product.ProductId);
        }
    }
}
