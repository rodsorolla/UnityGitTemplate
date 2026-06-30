using UnityEngine;

namespace Sorolla.Purchasing.Tests.Helpers
{
    /// <summary>Builds in-memory catalogs / products / rewards for unit tests.</summary>
    public static class TestCatalogFactory
    {
        public static CoinReward CoinReward(int amount)
        {
            var r = ScriptableObject.CreateInstance<CoinReward>();
            r.hideFlags = HideFlags.HideAndDontSave;
            typeof(CoinReward).GetField("_amount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(r, amount);
            return r;
        }

        public static EntitlementReward EntitlementReward(string key)
        {
            var r = ScriptableObject.CreateInstance<EntitlementReward>();
            r.hideFlags = HideFlags.HideAndDontSave;
            typeof(EntitlementReward).GetField("_entitlementKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(r, key);
            return r;
        }

        public static ProductDefinition Product(string id, PurchaseProductType type, params RewardDefinition[] rewards)
        {
            var p = ScriptableObject.CreateInstance<ProductDefinition>();
            p.hideFlags = HideFlags.HideAndDontSave;
            var t = typeof(ProductDefinition);
            void Set(string field, object value) =>
                t.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                  .SetValue(p, value);
            Set("_productId", id);
            Set("_type", type);
            Set("_rewards", new System.Collections.Generic.List<RewardDefinition>(rewards));
            return p;
        }

        public static PurchasingCatalog Catalog(params ProductDefinition[] products)
        {
            var c = ScriptableObject.CreateInstance<PurchasingCatalog>();
            c.hideFlags = HideFlags.HideAndDontSave;
            typeof(PurchasingCatalog).GetField("_products", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(c, new System.Collections.Generic.List<ProductDefinition>(products));
            return c;
        }
    }
}
