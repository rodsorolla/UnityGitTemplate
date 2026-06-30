using Sorolla.PersistentData;

namespace Sorolla.Purchasing
{
    /// <summary>
    /// SaveSystem-backed implementation. Loads the list once on construction; mutations
    /// write the entire (small) list back via SaveSystem (batched, no per-frame disk I/O
    /// because purchases happen at most a handful of times per session).
    /// </summary>
    public class ProcessedProductsStore : IProcessedProductsStore
    {
        public const string DefaultSaveFile = "processed_products";

        private readonly string _saveFile;
        private ProcessedProductsSaveData _data;

        public ProcessedProductsStore(string saveFile = DefaultSaveFile)
        {
            _saveFile = saveFile;
            _data = SaveSystem.Load<ProcessedProductsSaveData>(_saveFile);
        }

        public int Count => _data.Processed.Count;

        public bool Contains(string productId) =>
            !string.IsNullOrEmpty(productId) && _data.Processed.Contains(productId);

        public void MarkProcessed(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return;
            if (_data.Processed.Contains(productId)) return;
            _data.Processed.Add(productId);
            SaveSystem.Save(_data, _saveFile);
        }

        public void Reset()
        {
            _data = new ProcessedProductsSaveData();
            SaveSystem.Delete(_saveFile);
        }
    }
}
