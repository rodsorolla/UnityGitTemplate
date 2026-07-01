using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Sorolla.Purchasing.Tests
{
    public class PurchaseCompletedPanelTests
    {
        [Test]
        public void BuildRewardSummary_CoinsAndNoAds()
        {
            var coin = ScriptableObject.CreateInstance<CoinReward>();
            SetInt(coin, "_amount", 500);
            var noads = ScriptableObject.CreateInstance<EntitlementReward>();
            SetString(noads, "_entitlementKey", "noads");

            var product = ScriptableObject.CreateInstance<ProductDefinition>();
            var so = new SerializedObject(product);
            var list = so.FindProperty("_rewards");
            list.arraySize = 2;
            list.GetArrayElementAtIndex(0).objectReferenceValue = coin;
            list.GetArrayElementAtIndex(1).objectReferenceValue = noads;
            so.ApplyModifiedPropertiesWithoutUndo();

            var summary = PurchaseCompletedPanel.BuildRewardSummary(product);

            Assert.That(summary, Does.Contain("+500 coins"));
            Assert.That(summary, Does.Contain("No Ads unlocked"));

            Object.DestroyImmediate(coin);
            Object.DestroyImmediate(noads);
            Object.DestroyImmediate(product);
        }

        private static void SetInt(Object o, string prop, int v)
        {
            var so = new SerializedObject(o);
            so.FindProperty(prop).intValue = v;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object o, string prop, string v)
        {
            var so = new SerializedObject(o);
            so.FindProperty(prop).stringValue = v;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
