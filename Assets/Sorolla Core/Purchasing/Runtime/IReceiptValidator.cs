using Cysharp.Threading.Tasks;

namespace Sorolla.Purchasing
{
    public interface IReceiptValidator
    {
        UniTask<bool> ValidateAsync(PurchaseReceipt receipt);
    }
}
