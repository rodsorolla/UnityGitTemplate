using Sorolla.PersistentData;

namespace Sorolla.Purchasing
{
    /// <summary>
    /// Local, reinstall-wiped record of once-per-install purchases (e.g. a starter pack).
    /// Distinct from store entitlements, which restore across installs. Backed by a KVSaveData
    /// file written synchronously (iOS disk-I/O rule). Keyed via ProductDefinition.LocalPurchaseKey.
    /// </summary>
    public static class OncePerInstallStore
    {
        private const string SaveFile = "purchases";

        public static bool IsPurchased(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return false;
            var kv = SaveSystem.Load<KVSaveData>(SaveFile);
            return kv.Ints.TryGetValue(ProductDefinition.LocalPurchaseKey(productId), out var v) && v == 1;
        }

        public static void MarkPurchased(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return;
            var kv = SaveSystem.Load<KVSaveData>(SaveFile);
            kv.Ints[ProductDefinition.LocalPurchaseKey(productId)] = 1;
            SaveSystem.Save(kv, SaveFile);
        }
    }
}
