namespace Sorolla.Purchasing
{
    public readonly struct PurchaseReceipt
    {
        public string ProductId { get; }
        public string TransactionId { get; }
        public string ReceiptPayload { get; }
        public bool IsRestore { get; }

        public PurchaseReceipt(string productId, string transactionId, string receiptPayload, bool isRestore)
        {
            ProductId = productId;
            TransactionId = transactionId;
            ReceiptPayload = receiptPayload;
            IsRestore = isRestore;
        }
    }
}
