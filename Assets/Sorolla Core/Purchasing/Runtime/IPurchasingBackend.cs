using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Sorolla.Purchasing
{
    public interface IPurchasingBackend
    {
        bool IsReady { get; }
        string GetLocalizedPrice(string productId);

        UniTask<bool> InitializeAsync(IEnumerable<ProductDefinition> products);
        void InitiatePurchase(string productId);
        void Restore();

        /// <summary>
        /// Acknowledge a purchase to the store, called by the service only after the reward has
        /// been durably granted. Until this is called the order stays pending, so a crash before
        /// the grant persists makes the store re-deliver the purchase on next launch instead of
        /// leaving the player charged with nothing. Backends without a real store (mock) no-op.
        /// </summary>
        void FinalizePurchase(string transactionId);

        event Action OnReady;
        event Action OnInitFailed;
        event Action<PurchaseReceipt> OnPurchaseSucceeded;
        event Action<string, PurchaseFailureReason> OnPurchaseFailed;
        event Action<bool> OnRestoreCompleted;
    }
}
