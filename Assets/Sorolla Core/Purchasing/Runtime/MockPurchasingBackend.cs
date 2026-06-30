// Compiled only when it can actually be selected (see PurchasingService.SelectBackend),
// so the mock backend is never included in a release player build.
#if UNITY_EDITOR || SOROLLA_MOCK_PURCHASING
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sorolla.Purchasing
{
    /// <summary>
    /// Editor-only / SOROLLA_MOCK_PURCHASING-define-only fake. Simulates Unity IAP timing
    /// (default 200 ms purchase, 0 ms init) so async UI flows are exercisable without the
    /// store. The PurchasingDebugWindow drives the Force* and Simulate* methods.
    /// </summary>
    public class MockPurchasingBackend : IPurchasingBackend
    {
        private readonly Dictionary<string, ProductDefinition> _products = new();
        private readonly HashSet<string> _previouslyOwned = new();
        private bool _ready;

        public int InitDelayMs { get; set; } = 0;
        public int PurchaseDelayMs { get; set; } = 200;
        public bool ForceNextSuccess { get; set; } = true;
        public PurchaseFailureReason ForceNextFailure { get; set; } = PurchaseFailureReason.Unknown;
        public bool ForceNextFailureFlag { get; set; } = false;

        public bool IsReady => _ready;

        public event Action OnReady;
        public event Action OnInitFailed;
        public event Action<PurchaseReceipt> OnPurchaseSucceeded;
        public event Action<string, PurchaseFailureReason> OnPurchaseFailed;
        public event Action<bool> OnRestoreCompleted;

        public string GetLocalizedPrice(string productId) =>
            _products.ContainsKey(productId) ? "$0.99" : null;

        public async UniTask<bool> InitializeAsync(IEnumerable<ProductDefinition> products)
        {
            _products.Clear();
            foreach (var p in products)
            {
                if (p != null && !string.IsNullOrEmpty(p.ProductId))
                    _products[p.ProductId] = p;
            }
            if (InitDelayMs > 0) await UniTask.Delay(InitDelayMs, DelayType.UnscaledDeltaTime);
            _ready = true;
            OnReady?.Invoke();
            return true;
        }

        public void InitiatePurchase(string productId) => PurchaseAsync(productId).Forget();

        private async UniTaskVoid PurchaseAsync(string productId)
        {
            if (!_ready)
            {
                OnPurchaseFailed?.Invoke(productId, PurchaseFailureReason.NotInitialized);
                return;
            }
            if (!_products.ContainsKey(productId))
            {
                OnPurchaseFailed?.Invoke(productId, PurchaseFailureReason.ProductUnavailable);
                return;
            }
            // Ignore timescale: panels that pause gameplay set Time.timeScale = 0, which would
            // freeze a DeltaTime-based delay forever and hang the purchase (blocker never clears).
            if (PurchaseDelayMs > 0) await UniTask.Delay(PurchaseDelayMs, DelayType.UnscaledDeltaTime);

            if (ForceNextFailureFlag)
            {
                var reason = ForceNextFailure;
                ForceNextFailureFlag = false;
                ForceNextSuccess = true;
                OnPurchaseFailed?.Invoke(productId, reason);
                return;
            }

            var receipt = new PurchaseReceipt(productId, Guid.NewGuid().ToString(), "{}", isRestore: false);
            OnPurchaseSucceeded?.Invoke(receipt);
        }

        // No real store to acknowledge. Recorded so tests can assert the service finalizes only
        // after a durable grant (and never on validation failure).
        public HashSet<string> FinalizedTransactions { get; } = new HashSet<string>();

        public void FinalizePurchase(string transactionId)
        {
            if (!string.IsNullOrEmpty(transactionId)) FinalizedTransactions.Add(transactionId);
        }

        public void Restore()
        {
            foreach (var owned in _previouslyOwned)
            {
                var receipt = new PurchaseReceipt(owned, Guid.NewGuid().ToString(), "{}", isRestore: true);
                OnPurchaseSucceeded?.Invoke(receipt);
            }
            OnRestoreCompleted?.Invoke(true);
        }

        // ---- Dev menu hooks ----
        public void SimulatePreviouslyOwned(string productId) { if (!string.IsNullOrEmpty(productId)) _previouslyOwned.Add(productId); }
        public void ClearPreviouslyOwned() => _previouslyOwned.Clear();
        public void ForceNextSuccessOnly() { ForceNextSuccess = true; ForceNextFailureFlag = false; }
        public void ForceNextFailureWithReason(PurchaseFailureReason reason)
        {
            ForceNextFailureFlag = true;
            ForceNextFailure = reason;
            ForceNextSuccess = false;
        }
        public void SimulateInitFailure() => OnInitFailed?.Invoke();
    }
}
#endif
