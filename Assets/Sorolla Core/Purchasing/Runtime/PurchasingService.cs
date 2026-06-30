using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PaletteApi = Sorolla.Palette.Palette;
using UnityEngine;

namespace Sorolla.Purchasing
{
    /// <summary>
    /// Orchestrates the purchase pipeline: backend init → purchase request → receipt
    /// validation → reward dispatch → processed-product marking → analytics → events.
    /// All vendor-specific code lives behind IPurchasingBackend; analytics route through
    /// Palette per palette-boundary.md.
    /// </summary>
    public class PurchasingService : SorollaManager
    {
        [SerializeField] private PurchasingCatalog _catalog;

        private IPurchasingBackend _backend;
        private IReceiptValidator _validator;
        private IProcessedProductsStore _processed;
        private EntitlementService _entitlements;

        private readonly Dictionary<Type, Delegate> _handlers = new();

        public bool IsStoreReady => _backend != null && _backend.IsReady;
        public PurchasingCatalog Catalog => _catalog;

        public event Action<ProductDefinition, PurchaseRewardContext> OnPurchaseCompleted;
        // Fires after rewards are granted but BEFORE the store is acknowledged. Subscribers do
        // persistent purchase bookkeeping (e.g. once-per-install flags) and flush save state to
        // disk here, so the grant is durable before finalize. Keep this separate from
        // OnPurchaseCompleted, which is for UI/feedback after the purchase is fully settled.
        public event Action<ProductDefinition, PurchaseRewardContext> OnBeforePurchaseFinalize;
        public event Action<ProductDefinition, PurchaseFailureReason> OnPurchaseFailed;
        public event Action<ProductDefinition> OnPurchaseInitiated;
        public event Action OnStoreReady;
        public event Action<bool> OnRestoreCompleted;

        public string GetLocalizedPrice(string productId) => _backend?.GetLocalizedPrice(productId);

        public void InitiatePurchase(string productId)
        {
            if (string.IsNullOrEmpty(productId) || _backend == null) return;
            PaletteApi.TrackEvent("iap_buy_click", new Dictionary<string, object> { { "product_id", productId } });
            OnPurchaseInitiated?.Invoke(_catalog?.Find(productId));
            _backend.InitiatePurchase(productId);
        }

        public void Restore()
        {
            if (_backend == null) return;
            _backend.Restore();
        }

        /// <summary>
        /// Registers a handler for a concrete RewardDefinition subclass. EntitlementReward
        /// is auto-handled by the service and does not need (and ignores) a registration.
        /// </summary>
        public void RegisterRewardHandler<T>(Action<T, PurchaseRewardContext> handler) where T : RewardDefinition
        {
            if (handler == null) return;
            _handlers[typeof(T)] = handler;
        }

        protected override void Initialize()
        {
            _backend ??= SelectBackend();
            _validator ??= SelectValidator();
            _processed ??= new ProcessedProductsStore();
            _entitlements ??= ServiceLocator.Instance.TryResolve<EntitlementService>();

            _backend.OnReady += HandleReady;
            _backend.OnInitFailed += HandleInitFailed;
            _backend.OnPurchaseSucceeded += HandlePurchaseSucceeded;
            _backend.OnPurchaseFailed += HandlePurchaseFailed;
            _backend.OnRestoreCompleted += HandleRestoreCompleted;

            if (_catalog != null)
                _backend.InitializeAsync(_catalog.Products).Forget();

            ServiceLocator.Instance.Register(this);
        }

        private static IPurchasingBackend SelectBackend()
        {
#if UNITY_EDITOR || SOROLLA_MOCK_PURCHASING
            return new MockPurchasingBackend();
#else
            return new UnityIAPBackend();
#endif
        }

        // Receipt validation is intentionally OFF for our prototype-stage games (soft currency,
        // small audience): the no-op accepts the store receipt as-is. Local validation is a
        // bypassable speed bump rather than real fraud protection, and it carries per-project
        // Tangle-key + SOROLLA_IAP_VALIDATION-define setup we don't want mid-prototype. Revisit
        // (ideally server-side) only if a game scales to a large audience. The IReceiptValidator
        // seam stays so tests can inject a rejecting validator and validation can be plugged back
        // in later. See unity-projects/.claude/rules/iap-receipt-validation-policy.md.
        private static IReceiptValidator SelectValidator() => new NoOpReceiptValidator();

        private void HandleReady()
        {
            PaletteApi.TrackEvent("iap_store_ready");
            OnStoreReady?.Invoke();
        }

        private void HandleInitFailed() => PaletteApi.TrackEvent("iap_store_init_failed");

        private void HandleRestoreCompleted(bool success)
        {
            PaletteApi.TrackEvent(success ? "iap_restore_success" : "iap_restore_failed");
            OnRestoreCompleted?.Invoke(success);
        }

        private void HandlePurchaseSucceeded(PurchaseReceipt receipt)
        {
            ValidateAndDispatch(receipt).Forget();
        }

        private async UniTaskVoid ValidateAndDispatch(PurchaseReceipt receipt)
        {
            bool valid = await _validator.ValidateAsync(receipt);
            if (!valid)
            {
                Debug.LogWarning($"[PurchasingService] Receipt validation failed for {receipt.ProductId}.");
                PaletteApi.TrackEvent("iap_purchase_failed",
                    new Dictionary<string, object> { { "product_id", receipt.ProductId }, { "reason", "ReceiptInvalid" } });
                return;
            }

            var product = _catalog?.Find(receipt.ProductId);
            if (product == null)
            {
                Debug.LogWarning($"[PurchasingService] Unknown productId: {receipt.ProductId}");
                return;
            }

            bool isFirstTime = product.Type == PurchaseProductType.Consumable
                ? true
                : !_processed.Contains(product.ProductId);

            var ctx = new PurchaseRewardContext(product, receipt.IsRestore, isFirstTime);

            bool allGranted = true;
            foreach (var reward in product.Rewards)
            {
                if (reward == null) continue;
                if (reward.Policy == GrantPolicy.EveryPurchase && !isFirstTime) continue;
                if (!DispatchReward(reward, ctx)) allGranted = false;
            }

            // A reward that should have been granted was not (no handler / handler threw). Do NOT
            // acknowledge the purchase: leave the order pending so the store re-delivers it, and
            // surface the failure loudly rather than silently acking a non-grant (charged, nothing).
            if (!allGranted)
            {
                Debug.LogError($"[PurchasingService] Reward grant incomplete for {product.ProductId}; " +
                    $"not finalizing purchase. Store will re-deliver. Check reward handler registration.");
                PaletteApi.TrackEvent("iap_grant_incomplete",
                    new Dictionary<string, object> { { "product_id", product.ProductId } });
                return;
            }

            // Durability checkpoint: subscribers persist purchase bookkeeping and flush save
            // state to disk. Run BEFORE finalize so a crash after ack cannot lose granted value.
            // If a subscriber throws, the grant is not durable: skip finalize, leave order pending.
            try
            {
                OnBeforePurchaseFinalize?.Invoke(product, ctx);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PurchasingService] Pre-finalize checkpoint threw for {product.ProductId}; " +
                    $"not finalizing purchase. Store will re-deliver. {ex}");
                PaletteApi.TrackEvent("iap_grant_incomplete",
                    new Dictionary<string, object> { { "product_id", product.ProductId }, { "reason", "checkpoint_threw" } });
                return;
            }

            if (product.Type == PurchaseProductType.NonConsumable)
                _processed.MarkProcessed(product.ProductId);

            if (isFirstTime && !receipt.IsRestore)
            {
                // Real $/currency tracking is owned by Palette.AttachPurchaseTracking (wired in
                // UnityIAPBackend on the OnPurchasePending path); we additionally emit a
                // non-economic event for analytics funnel tracking (mock backend has no store).
                PaletteApi.TrackEvent("iap_purchase_completed",
                    new Dictionary<string, object> { { "product_id", product.ProductId } });
            }

            // Reward is granted and durably checkpointed. Acknowledge to the store now. Restores
            // are already store-confirmed, so they are not re-finalized. The early-return paths
            // above (invalid receipt, unknown product, incomplete grant) never finalize.
            if (!receipt.IsRestore)
                _backend.FinalizePurchase(receipt.TransactionId);

            OnPurchaseCompleted?.Invoke(product, ctx);
        }

        // Returns true only if the reward was actually granted. A missing handler or a throwing
        // handler returns false so the caller can withhold purchase finalization.
        private bool DispatchReward(RewardDefinition reward, PurchaseRewardContext ctx)
        {
            if (reward is EntitlementReward er)
            {
                if (_entitlements != null && !string.IsNullOrEmpty(er.Key))
                {
                    _entitlements.Grant(er.Key);
                    return true;
                }
                Debug.LogError($"[PurchasingService] EntitlementReward could not be granted " +
                    $"(entitlement service missing or empty key).");
                return false;
            }

            if (_handlers.TryGetValue(reward.GetType(), out var del))
            {
                try
                {
                    del.DynamicInvoke(reward, ctx);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PurchasingService] Reward handler for {reward.GetType().Name} threw: {ex}");
                    return false;
                }
            }

            Debug.LogError($"[PurchasingService] No handler registered for reward type {reward.GetType().Name}.");
            return false;
        }

        private void HandlePurchaseFailed(string productId, PurchaseFailureReason reason)
        {
            PaletteApi.TrackEvent("iap_purchase_failed",
                new Dictionary<string, object>
                {
                    { "product_id", productId ?? "<null>" },
                    { "reason", reason.ToString() }
                });
            var product = _catalog?.Find(productId);
            OnPurchaseFailed?.Invoke(product, reason);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static PurchasingService CreateForTests(
            PurchasingCatalog catalog,
            IPurchasingBackend backend,
            EntitlementService entitlements,
            IProcessedProductsStore processed,
            IReceiptValidator validator)
        {
            var go = new GameObject("PurchasingService_Test") { hideFlags = HideFlags.HideAndDontSave };
            var svc = go.AddComponent<PurchasingService>();
            svc._catalog = catalog;
            svc._backend = backend;
            svc._entitlements = entitlements;
            svc._processed = processed;
            svc._validator = validator;
            svc.Init();
            return svc;
        }

        public IPurchasingBackend BackendForTests => _backend;
#endif
    }
}
