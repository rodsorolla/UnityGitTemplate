namespace Sorolla.Purchasing
{
    public interface IProcessedProductsStore
    {
        bool Contains(string productId);
        void MarkProcessed(string productId);
        int Count { get; }
        void Reset();
    }
}
