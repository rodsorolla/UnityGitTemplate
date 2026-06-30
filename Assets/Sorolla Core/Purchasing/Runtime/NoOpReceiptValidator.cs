using Cysharp.Threading.Tasks;

namespace Sorolla.Purchasing
{
    public class NoOpReceiptValidator : IReceiptValidator
    {
        public UniTask<bool> ValidateAsync(PurchaseReceipt receipt) => UniTask.FromResult(true);
    }
}
