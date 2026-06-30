namespace Sorolla.Purchasing
{
    public enum PurchaseFailureReason
    {
        Unknown,
        UserCancelled,
        PaymentDeclined,
        NetworkError,
        ProductUnavailable,
        DuplicateTransaction,
        NotInitialized
    }
}
