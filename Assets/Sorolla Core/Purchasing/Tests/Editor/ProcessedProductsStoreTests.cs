using NUnit.Framework;
using Sorolla.PersistentData;

namespace Sorolla.Purchasing.Tests
{
    public class ProcessedProductsStoreTests
    {
        private const string TestSaveFile = "purchasing_processed_test";

        [SetUp]
        public void Setup() => SaveSystem.Delete(TestSaveFile);

        [TearDown]
        public void Teardown() => SaveSystem.Delete(TestSaveFile);

        [Test]
        public void FreshStore_HasNothingProcessed()
        {
            var store = new ProcessedProductsStore(TestSaveFile);
            Assert.That(store.Contains("any.product"), Is.False);
        }

        [Test]
        public void MarkProcessed_PersistsAcrossInstances()
        {
            var first = new ProcessedProductsStore(TestSaveFile);
            first.MarkProcessed("com.test.bundle");

            var second = new ProcessedProductsStore(TestSaveFile);
            Assert.That(second.Contains("com.test.bundle"), Is.True);
        }

        [Test]
        public void MarkProcessed_IsIdempotent()
        {
            var store = new ProcessedProductsStore(TestSaveFile);
            store.MarkProcessed("com.test.bundle");
            store.MarkProcessed("com.test.bundle");
            store.MarkProcessed("com.test.bundle");

            var reloaded = new ProcessedProductsStore(TestSaveFile);
            Assert.That(reloaded.Count, Is.EqualTo(1));
        }

        [Test]
        public void Reset_ClearsAllAndDeletesFile()
        {
            var store = new ProcessedProductsStore(TestSaveFile);
            store.MarkProcessed("a");
            store.MarkProcessed("b");
            store.Reset();

            Assert.That(store.Contains("a"), Is.False);
            Assert.That(SaveSystem.Exists(TestSaveFile), Is.False);
        }
    }
}
