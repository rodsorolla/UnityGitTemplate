using NUnit.Framework;
using Sorolla.PersistentData;

namespace Sorolla.Purchasing.Tests
{
    public class OncePerInstallStoreTests
    {
        private const string SaveFile = "purchases";

        [SetUp]    public void Setup()    => SaveSystem.Delete(SaveFile);
        [TearDown] public void Teardown() => SaveSystem.Delete(SaveFile);

        [Test]
        public void FreshInstall_NotPurchased()
        {
            Assert.That(OncePerInstallStore.IsPurchased("prod_x"), Is.False);
        }

        [Test]
        public void MarkPurchased_ThenIsPurchased()
        {
            OncePerInstallStore.MarkPurchased("prod_x");
            Assert.That(OncePerInstallStore.IsPurchased("prod_x"), Is.True);
        }

        [Test]
        public void MarkPurchased_DoesNotAffectOtherProduct()
        {
            OncePerInstallStore.MarkPurchased("prod_x");
            Assert.That(OncePerInstallStore.IsPurchased("prod_y"), Is.False);
        }
    }
}
