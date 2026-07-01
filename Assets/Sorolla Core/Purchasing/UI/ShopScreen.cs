using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sorolla.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Purchasing
{
    /// <summary>
    /// Catalog-driven shop screen. Builds one ProductCard per ProductDefinition into the
    /// section RectTransform matching the product's ShopSection. Cards stay in sync via
    /// OnPurchaseCompleted / OnEntitlementChanged / OnStoreReady. Pushed via UIScreenId.Shop.
    /// </summary>
    public class ShopScreen : UIScreen
    {
        [Header("Section containers")]
        [SerializeField] private RectTransform _bundleSection;
        [SerializeField] private RectTransform _starterPackSection;
        [SerializeField] private RectTransform _noAdsSection;
        [SerializeField] private RectTransform _coinPacksSection;

        [Header("Navigation")]
        [SerializeField] private Button _backButton;

        [Header("Restore (Apple App Store requirement)")]
        [SerializeField] private Button _restorePurchasesButton;

        [Header("Purchase in-flight blocker")]
        [Tooltip("Full-stretch overlay with raycastTarget=true. Shown while an IAP transaction is in progress. Hidden on success/fail/cancel.")]
        [SerializeField] private GameObject _purchaseBlocker;

        private PurchasingService _purchasing;
        private EntitlementService _entitlements;
        private readonly List<ProductCard> _cards = new();

        private void Awake()
        {
            if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnEnable()
        {
            _purchasing ??= ServiceLocator.Instance.TryResolve<PurchasingService>();
            _entitlements ??= ServiceLocator.Instance.TryResolve<EntitlementService>();

            if (_restorePurchasesButton != null)
                _restorePurchasesButton.onClick.AddListener(OnRestoreClicked);

            if (_purchasing != null)
            {
                _purchasing.OnPurchaseCompleted += HandlePurchaseCompleted;
                _purchasing.OnPurchaseFailed += HandlePurchaseFailed;
                _purchasing.OnPurchaseInitiated += HandlePurchaseInitiated;
                _purchasing.OnStoreReady += RefreshAll;
            }
            if (_entitlements != null)
                _entitlements.OnEntitlementChanged += HandleEntitlementChanged;

            SetBlockerVisible(false);
            BuildCards();
            RefreshAll();
        }

        private void OnDisable()
        {
            if (_restorePurchasesButton != null)
                _restorePurchasesButton.onClick.RemoveListener(OnRestoreClicked);
            if (_purchasing != null)
            {
                _purchasing.OnPurchaseCompleted -= HandlePurchaseCompleted;
                _purchasing.OnPurchaseFailed -= HandlePurchaseFailed;
                _purchasing.OnPurchaseInitiated -= HandlePurchaseInitiated;
                _purchasing.OnStoreReady -= RefreshAll;
            }
            if (_entitlements != null)
                _entitlements.OnEntitlementChanged -= HandleEntitlementChanged;

            SetBlockerVisible(false);
            TearDownCards();
        }

        private void OnBackClicked() => UIManager.Instance.PopScreenAsync().Forget();

        private void BuildCards()
        {
            TearDownCards();
            if (_purchasing == null || _purchasing.Catalog == null) return;

            foreach (var product in _purchasing.Catalog.Products)
            {
                if (product == null || product.CardPrefab == null) continue;
                var parent = ResolveParent(product.ShopSection);
                if (parent == null) continue;
                var instance = Instantiate(product.CardPrefab, parent);
                var card = instance.GetComponent<ProductCard>();
                if (card == null)
                {
                    Debug.LogWarning($"[ShopScreen] Card prefab for {product.ProductId} is missing a ProductCard component.");
                    Destroy(instance);
                    continue;
                }
                card.Bind(product, _purchasing, _entitlements);
                _cards.Add(card);
            }

            RefreshSectionVisibility();
        }

        // Hides a section once every card inside it has hidden itself (entitlement owned,
        // redundant, or once-per-install already bought). A section with no cards at all
        // stays visible (it may simply be empty in this catalog).
        private void RefreshSectionVisibility()
        {
            SetSectionVisible(_bundleSection);
            SetSectionVisible(_starterPackSection);
            SetSectionVisible(_noAdsSection);
            SetSectionVisible(_coinPacksSection);
        }

        private void SetSectionVisible(RectTransform section)
        {
            if (section == null) return;

            bool anyVisibleCard = false;
            bool anyCardAtAll = false;
            foreach (var card in _cards)
            {
                if (card == null || card.transform.parent != section.transform) continue;
                anyCardAtAll = true;
                if (card.gameObject.activeSelf) { anyVisibleCard = true; break; }
            }
            section.gameObject.SetActive(!anyCardAtAll || anyVisibleCard);
        }

        private RectTransform ResolveParent(ShopSection section) => section switch
        {
            ShopSection.Bundle => _bundleSection,
            ShopSection.StarterPack => _starterPackSection,
            ShopSection.NoAds => _noAdsSection,
            ShopSection.CoinPacks => _coinPacksSection,
            _ => null,
        };

        private void TearDownCards()
        {
            foreach (var c in _cards)
                if (c != null) Destroy(c.gameObject);
            _cards.Clear();
        }

        private void RefreshAll()
        {
            foreach (var c in _cards) c?.Refresh();
            RefreshSectionVisibility();
        }

        private void OnRestoreClicked() => _purchasing?.Restore();
        private void HandlePurchaseInitiated(ProductDefinition _) => SetBlockerVisible(true);
        private void HandlePurchaseFailed(ProductDefinition _, PurchaseFailureReason __) => SetBlockerVisible(false);

        private void SetBlockerVisible(bool visible)
        {
            if (_purchaseBlocker != null) _purchaseBlocker.SetActive(visible);
        }

        private void HandlePurchaseCompleted(ProductDefinition product, PurchaseRewardContext ctx)
        {
            SetBlockerVisible(false);
            RefreshAll();
            // Fresh first-time purchase only — restore re-fires shouldn't pop the panel again.
            if (product != null && ctx.IsFirstTime && !ctx.IsRestore)
            {
                var data = new PurchaseCompletedPanel.PurchaseCompletedData(product);
                UIManager.Instance?.OpenPanelAsync(UIPanelId.PurchaseCompleted, data).Forget();
            }
        }

        private void HandleEntitlementChanged(string _, bool __) => RefreshAll();
    }
}
