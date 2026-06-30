using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using PaletteApi = Sorolla.Palette.Palette;

namespace Sorolla.Purchasing
{
    /// <summary>
    /// Real-store backend built on Unity IAP v5 (UnityIAPServices.StoreController +
    /// order-based callbacks). Analytics are wired by Palette.AttachPurchaseTracking,
    /// which owns the OnPurchasePending subscription and runs TxID dedup internally, so
    /// game code never fires Palette.TrackPurchase directly. This backend only handles
    /// fulfillment (grant + confirm) and restore, behind the IPurchasingBackend contract.
    /// </summary>
    public class UnityIAPBackend : IPurchasingBackend
    {
        private StoreController _store;
        private bool _ready;
        private bool _restoreRequested;
        private UniTaskCompletionSource<bool> _initTcs;

        // Pending orders awaiting durable grant, keyed by transaction id. We acknowledge the
        // store (ConfirmPurchase) only once the service calls FinalizePurchase, so a crash
        // between "paid" and "reward saved" leaves the order pending for re-delivery.
        private readonly Dictionary<string, PendingOrder> _pendingByTx = new Dictionary<string, PendingOrder>();

        public bool IsReady => _ready;

        public event Action OnReady;
        public event Action OnInitFailed;
        public event Action<PurchaseReceipt> OnPurchaseSucceeded;
        public event Action<string, PurchaseFailureReason> OnPurchaseFailed;
        public event Action<bool> OnRestoreCompleted;

        public string GetLocalizedPrice(string productId) =>
            _store?.GetProductById(productId)?.metadata?.localizedPriceString;

        public async UniTask<bool> InitializeAsync(IEnumerable<ProductDefinition> products)
        {
            _initTcs = new UniTaskCompletionSource<bool>();

            _store = UnityIAPServices.StoreController();

            // Palette owns the OnPurchasePending analytics subscription + TxID dedup. Wire it
            // before Connect so no early purchase callback escapes tracking.
            PaletteApi.AttachPurchaseTracking(_store);

            _store.OnPurchasePending += HandlePurchasePending;
            _store.OnPurchaseConfirmed += HandlePurchaseConfirmed;
            _store.OnPurchaseFailed += HandlePurchaseFailed;
            _store.OnProductsFetched += HandleProductsFetched;
            _store.OnProductsFetchFailed += HandleProductsFetchFailed;
            _store.OnPurchasesFetched += HandlePurchasesFetched;
            _store.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;

            var defs = new List<UnityEngine.Purchasing.ProductDefinition>();
            foreach (ProductDefinition p in products)
            {
                if (p == null || string.IsNullOrEmpty(p.ProductId)) continue;
                ProductType unityType = p.Type == PurchaseProductType.Consumable
                    ? ProductType.Consumable : ProductType.NonConsumable;
                string storeId = ResolveStoreSpecificId(p);
                defs.Add(new UnityEngine.Purchasing.ProductDefinition(p.ProductId, storeId, unityType));
            }

            try
            {
                await _store.Connect().AsUniTask();
            }
            catch (Exception e)
            {
                HandleInitFailed($"Connect failed: {e.Message}");
                return false;
            }

            _store.FetchProducts(defs);
            return await _initTcs.Task;
        }

        public void InitiatePurchase(string productId)
        {
            if (!_ready)
            {
                OnPurchaseFailed?.Invoke(productId, PurchaseFailureReason.NotInitialized);
                return;
            }
            _store.PurchaseProduct(productId);
        }

        public void Restore()
        {
            if (_store == null)
            {
                OnRestoreCompleted?.Invoke(false);
                return;
            }

            _restoreRequested = true;

            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                _store.RestoreTransactions((success, _) =>
                {
                    // Unity IAP v5 calls FetchPurchases internally after a successful restore.
                    // OnPurchasesFetched will complete the restore flow while _restoreRequested
                    // is still true.
                    if (!success)
                    {
                        _restoreRequested = false;
                        OnRestoreCompleted?.Invoke(false);
                    }
                });
            }
            else
            {
                _store.FetchPurchases();
            }
        }

        private static string ResolveStoreSpecificId(ProductDefinition p)
        {
            string platformId = Application.platform == RuntimePlatform.IPhonePlayer
                ? p.GetStoreSpecificId(RuntimePlatform.IPhonePlayer)
                : p.GetStoreSpecificId(RuntimePlatform.Android);
            return string.IsNullOrEmpty(platformId) ? p.ProductId : platformId;
        }

        private void HandleProductsFetched(List<Product> products)
        {
            _ready = true;
            // Ask Unity IAP to re-deliver any unfinished pending purchases on startup. The v5
            // purchase service routes fetched PendingOrders through OnPurchasePending before it
            // raises OnPurchasesFetched; confirmed restore handling below stays gated by
            // _restoreRequested so normal startup does not emit restore completion.
            _store.FetchPurchases();
            OnReady?.Invoke();
            _initTcs?.TrySetResult(true);
        }

        private void HandleProductsFetchFailed(ProductFetchFailed failure) =>
            HandleInitFailed($"Products fetch failed: {failure?.FailureReason}");

        private void HandleInitFailed(string detail)
        {
            Debug.LogWarning($"[UnityIAPBackend] Init failed: {detail}");
            OnInitFailed?.Invoke();
            _initTcs?.TrySetResult(false);
        }

        private void HandlePurchasePending(PendingOrder order)
        {
            Product product = ProductOf(order);
            string transactionId = order?.Info?.TransactionID;

            // The handshake correlates pending -> grant -> finalize by transaction id. Without one
            // we cannot safely defer, so we do NOT grant or confirm: leave the order pending and let
            // the store re-deliver it (a real Google/Apple purchase always carries a transaction id).
            if (string.IsNullOrEmpty(transactionId))
            {
                Debug.LogWarning("[UnityIAPBackend] PendingOrder has no transaction id; skipping grant and ack. Order stays pending for re-delivery.");
                return;
            }

            // Cache the order and confirm only once the service has durably granted the reward and
            // calls FinalizePurchase. Analytics fire regardless (owned by Palette's own
            // OnPurchasePending subscription, wired by AttachPurchaseTracking).
            _pendingByTx[transactionId] = order;

            if (product != null)
            {
                var receipt = new PurchaseReceipt(
                    product.definition.id,
                    transactionId,
                    order?.Info?.Receipt,
                    isRestore: false);
                OnPurchaseSucceeded?.Invoke(receipt);
            }
        }

        // Asks the store to acknowledge the order. We keep it in _pendingByTx until the store
        // reports ConfirmedOrder (see HandlePurchaseConfirmed); on ack failure it stays cached and
        // pending so it can be retried, per Unity IAP v5's order state machine.
        public void FinalizePurchase(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return;
            if (_pendingByTx.TryGetValue(transactionId, out PendingOrder order))
                _store.ConfirmPurchase(order);
            // No cached order (already confirmed, or a restore which is store-confirmed): no-op.
        }

        // v5 delivers the confirmation result here for both outcomes: a ConfirmedOrder on
        // acknowledgement success, a FailedOrder on ack failure (NOT via OnPurchaseFailed,
        // which only covers purchase-initiation failures). Reward is already granted on the
        // pending path; this only adds observability so a silent ack failure (which triggers
        // a store-side auto-refund) is visible instead of vanishing.
        private void HandlePurchaseConfirmed(Order order)
        {
            if (order is FailedOrder failed)
            {
                // Ack failed: keep the order in _pendingByTx so it stays retryable (the store will
                // also re-deliver an unconfirmed order). Do not drop the retry handle here.
                Product product = ProductOf(failed);
                string productId = product?.definition?.id;
                Debug.LogWarning($"[UnityIAPBackend] Purchase confirm/ack FAILED for " +
                    $"'{productId}': {failed.FailureReason} ({failed.Details}). Order stays pending for retry.");

                string details = failed.Details ?? string.Empty;
                if (details.Length > 200) details = details.Substring(0, 200);
                PaletteApi.TrackEvent("iap_purchase_confirm_failed", new Dictionary<string, object>
                {
                    { "product_id", productId ?? "unknown" },
                    { "reason", failed.FailureReason.ToString() },
                    { "details", details },
                });
                return;
            }

            // Ack succeeded: the order is settled, drop the retry handle.
            string txId = order?.Info?.TransactionID;
            if (!string.IsNullOrEmpty(txId)) _pendingByTx.Remove(txId);
        }

        private void HandlePurchaseFailed(FailedOrder order)
        {
            Product product = ProductOf(order);
            OnPurchaseFailed?.Invoke(product?.definition?.id, MapReason(order?.FailureReason ?? UnityEngine.Purchasing.PurchaseFailureReason.Unknown));
        }

        private void HandlePurchasesFetched(Orders orders)
        {
            if (!_restoreRequested) return;
            _restoreRequested = false;

            foreach (ConfirmedOrder order in orders.ConfirmedOrders)
            {
                Product product = ProductOf(order);
                if (product == null) continue;
                if (product.definition.type == ProductType.Consumable) continue;
                var receipt = new PurchaseReceipt(
                    product.definition.id,
                    order.Info?.TransactionID,
                    order.Info?.Receipt,
                    isRestore: true);
                OnPurchaseSucceeded?.Invoke(receipt);
            }

            OnRestoreCompleted?.Invoke(true);
        }

        private void HandlePurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            if (!_restoreRequested) return;
            _restoreRequested = false;
            Debug.LogWarning($"[UnityIAPBackend] Restore fetch failed: {failure?.FailureReason} ({failure?.Message}).");
            OnRestoreCompleted?.Invoke(false);
        }

        private static Product ProductOf(Order order)
        {
            IReadOnlyList<CartItem> items = order?.CartOrdered?.Items();
            return items != null && items.Count > 0 ? items[0].Product : null;
        }

        private static PurchaseFailureReason MapReason(UnityEngine.Purchasing.PurchaseFailureReason r) => r switch
        {
            UnityEngine.Purchasing.PurchaseFailureReason.UserCancelled => PurchaseFailureReason.UserCancelled,
            UnityEngine.Purchasing.PurchaseFailureReason.PaymentDeclined => PurchaseFailureReason.PaymentDeclined,
            UnityEngine.Purchasing.PurchaseFailureReason.ExistingPurchasePending => PurchaseFailureReason.DuplicateTransaction,
            UnityEngine.Purchasing.PurchaseFailureReason.ProductUnavailable => PurchaseFailureReason.ProductUnavailable,
            UnityEngine.Purchasing.PurchaseFailureReason.PurchasingUnavailable => PurchaseFailureReason.NotInitialized,
            _ => PurchaseFailureReason.Unknown,
        };
    }
}
