using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Sorolla;
using Sorolla.PersistentData;
using Sorolla.Purchasing.Tests.Helpers;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sorolla.Purchasing.Tests
{
    public class PurchasingServiceTests
    {
        private const string ProcFile = "purchasing_proc_test";
        private const string EntFile = "purchasing_ent_test";
        private const string LegacyFile = "iap_unused";

        private MockPurchasingBackend _backend;
        private PurchasingService _svc;
        private EntitlementService _ent;
        private ProcessedProductsStore _proc;

        [SetUp]
        public void Setup()
        {
            ServiceLocator.Reset();
            SaveSystem.Delete(ProcFile);
            SaveSystem.Delete(EntFile);
            _backend = new MockPurchasingBackend { PurchaseDelayMs = 0, InitDelayMs = 0 };
            _ent = EntitlementService.CreateForTests(EntFile, LegacyFile);
            _proc = new ProcessedProductsStore(ProcFile);
        }

        [TearDown]
        public void Teardown()
        {
            if (_svc != null) Object.DestroyImmediate(_svc.gameObject);
            if (_ent != null) Object.DestroyImmediate(_ent.gameObject);
            SaveSystem.Delete(ProcFile);
            SaveSystem.Delete(EntFile);
            ServiceLocator.Reset();
        }

        private async UniTask InitService(PurchasingCatalog catalog)
        {
            _svc = PurchasingService.CreateForTests(catalog, _backend, _ent, _proc, new NoOpReceiptValidator());
            await UniTask.Yield();   // allow InitializeAsync to complete
        }

        [UnityTest]
        public System.Collections.IEnumerator Consumable_FirstPurchase_GrantsCoinsAndDoesNotMarkProcessed() => UniTask.ToCoroutine(async () =>
        {
            var coins = TestCatalogFactory.CoinReward(1000);
            var product = TestCatalogFactory.Product("c.coins", PurchaseProductType.Consumable, coins);
            var catalog = TestCatalogFactory.Catalog(product);
            int totalCoinsGranted = 0;

            await InitService(catalog);
            _svc.RegisterRewardHandler<CoinReward>((r, _) => totalCoinsGranted += r.Amount);

            _svc.InitiatePurchase("c.coins");
            await UniTask.Yield();
            await UniTask.Yield();

            Assert.That(totalCoinsGranted, Is.EqualTo(1000));
            Assert.That(_proc.Contains("c.coins"), Is.False);     // consumables not tracked
        });

        [UnityTest]
        public System.Collections.IEnumerator NonConsumable_FirstPurchase_GrantsEntitlementAndMarksProcessed() => UniTask.ToCoroutine(async () =>
        {
            var ent = TestCatalogFactory.EntitlementReward("noads");
            var product = TestCatalogFactory.Product("c.noads", PurchaseProductType.NonConsumable, ent);
            await InitService(TestCatalogFactory.Catalog(product));

            _svc.InitiatePurchase("c.noads");
            await UniTask.Yield();
            await UniTask.Yield();

            Assert.That(_ent.Has("noads"), Is.True);
            Assert.That(_proc.Contains("c.noads"), Is.True);
        });

        [UnityTest]
        public System.Collections.IEnumerator MixedBundle_FirstPurchase_GrantsBothRewards() => UniTask.ToCoroutine(async () =>
        {
            var ent = TestCatalogFactory.EntitlementReward("noads");
            var coins = TestCatalogFactory.CoinReward(3000);
            var bundle = TestCatalogFactory.Product("c.bundle", PurchaseProductType.NonConsumable, ent, coins);
            await InitService(TestCatalogFactory.Catalog(bundle));

            int coinsGranted = 0;
            _svc.RegisterRewardHandler<CoinReward>((r, _) => coinsGranted += r.Amount);

            _svc.InitiatePurchase("c.bundle");
            await UniTask.Yield();
            await UniTask.Yield();

            Assert.That(_ent.Has("noads"), Is.True);
            Assert.That(coinsGranted, Is.EqualTo(3000));
            Assert.That(_proc.Contains("c.bundle"), Is.True);
        });

        [UnityTest]
        public System.Collections.IEnumerator MixedBundle_RestoreReFire_OnlyRegrantsEntitlement() => UniTask.ToCoroutine(async () =>
        {
            var ent = TestCatalogFactory.EntitlementReward("noads");
            var coins = TestCatalogFactory.CoinReward(3000);
            var bundle = TestCatalogFactory.Product("c.bundle", PurchaseProductType.NonConsumable, ent, coins);
            await InitService(TestCatalogFactory.Catalog(bundle));

            // Pretend the user already bought the bundle in a previous install:
            _proc.MarkProcessed("c.bundle");
            _ent.Revoke("noads"); // fresh device — entitlement file blank

            int coinsGranted = 0;
            _svc.RegisterRewardHandler<CoinReward>((r, _) => coinsGranted += r.Amount);

            _backend.SimulatePreviouslyOwned("c.bundle");
            _svc.Restore();
            await UniTask.Yield();
            await UniTask.Yield();

            Assert.That(_ent.Has("noads"), Is.True);    // re-granted via OncePerProduct policy
            Assert.That(coinsGranted, Is.EqualTo(0));   // EveryPurchase + !IsFirstTime → suppressed
        });

        [UnityTest]
        public System.Collections.IEnumerator FailedPurchase_FiresFailedEventAndDoesNotMutateState() => UniTask.ToCoroutine(async () =>
        {
            var coins = TestCatalogFactory.CoinReward(1000);
            var product = TestCatalogFactory.Product("c.coins", PurchaseProductType.Consumable, coins);
            await InitService(TestCatalogFactory.Catalog(product));

            int coinsGranted = 0;
            int failures = 0;
            _svc.RegisterRewardHandler<CoinReward>((r, _) => coinsGranted += r.Amount);
            _svc.OnPurchaseFailed += (_, _) => failures++;

            _backend.ForceNextFailureWithReason(PurchaseFailureReason.PaymentDeclined);
            _svc.InitiatePurchase("c.coins");
            await UniTask.Yield();
            await UniTask.Yield();

            Assert.That(coinsGranted, Is.EqualTo(0));
            Assert.That(failures, Is.EqualTo(1));
            Assert.That(_proc.Contains("c.coins"), Is.False);
        });

        [UnityTest]
        public System.Collections.IEnumerator SuccessfulPurchase_FinalizesAfterGrant() => UniTask.ToCoroutine(async () =>
        {
            var coins = TestCatalogFactory.CoinReward(1000);
            var product = TestCatalogFactory.Product("c.coins", PurchaseProductType.Consumable, coins);
            await InitService(TestCatalogFactory.Catalog(product));
            _svc.RegisterRewardHandler<CoinReward>((r, _) => { });   // handler present -> grant succeeds

            _svc.InitiatePurchase("c.coins");
            await UniTask.Yield();
            await UniTask.Yield();

            Assert.That(_backend.FinalizedTransactions.Count, Is.EqualTo(1));
        });

        [UnityTest]
        public System.Collections.IEnumerator PreFinalizeCheckpoint_RunsBeforeFinalize() => UniTask.ToCoroutine(async () =>
        {
            var coins = TestCatalogFactory.CoinReward(1000);
            var product = TestCatalogFactory.Product("c.coins", PurchaseProductType.Consumable, coins);
            await InitService(TestCatalogFactory.Catalog(product));
            _svc.RegisterRewardHandler<CoinReward>((r, _) => { });

            bool checkpointRan = false;
            _svc.OnBeforePurchaseFinalize += (_, _) =>
            {
                Assert.That(_backend.FinalizedTransactions.Count, Is.EqualTo(0));
                checkpointRan = true;
            };

            _svc.InitiatePurchase("c.coins");
            await UniTask.Yield();
            await UniTask.Yield();

            Assert.That(checkpointRan, Is.True);
            Assert.That(_backend.FinalizedTransactions.Count, Is.EqualTo(1));
        });

        [UnityTest]
        public System.Collections.IEnumerator PreFinalizeCheckpointThrow_DoesNotFinalizeOrComplete() => UniTask.ToCoroutine(async () =>
        {
            var coins = TestCatalogFactory.CoinReward(1000);
            var product = TestCatalogFactory.Product("c.coins", PurchaseProductType.Consumable, coins);
            await InitService(TestCatalogFactory.Catalog(product));
            _svc.RegisterRewardHandler<CoinReward>((r, _) => { });

            bool completed = false;
            _svc.OnBeforePurchaseFinalize += (_, _) => throw new System.Exception("checkpoint boom");
            _svc.OnPurchaseCompleted += (_, _) => completed = true;

            LogAssert.ignoreFailingMessages = true;
            _svc.InitiatePurchase("c.coins");
            await UniTask.Yield();
            await UniTask.Yield();
            LogAssert.ignoreFailingMessages = false;

            Assert.That(_backend.FinalizedTransactions.Count, Is.EqualTo(0));
            Assert.That(completed, Is.False);
        });

        [UnityTest]
        public System.Collections.IEnumerator NonConsumable_CheckpointThrow_DoesNotMarkProcessed() => UniTask.ToCoroutine(async () =>
        {
            var ent = TestCatalogFactory.EntitlementReward("vip");
            var product = TestCatalogFactory.Product("c.vip", PurchaseProductType.NonConsumable, ent);
            await InitService(TestCatalogFactory.Catalog(product));

            _svc.OnBeforePurchaseFinalize += (_, _) => throw new System.Exception("checkpoint boom");

            LogAssert.ignoreFailingMessages = true;
            _svc.InitiatePurchase("c.vip");
            await UniTask.Yield();
            await UniTask.Yield();
            LogAssert.ignoreFailingMessages = false;

            Assert.That(_backend.FinalizedTransactions.Count, Is.EqualTo(0));
            Assert.That(_proc.Contains("c.vip"), Is.False);
        });

        [UnityTest]
        public System.Collections.IEnumerator MissingHandler_GrantsNothingAndDoesNotFinalize() => UniTask.ToCoroutine(async () =>
        {
            // CoinReward with NO registered handler: nothing is granted, so the purchase must
            // not be finalized (store re-delivers instead of charging for an ungranted reward).
            var coins = TestCatalogFactory.CoinReward(1000);
            var product = TestCatalogFactory.Product("c.coins", PurchaseProductType.Consumable, coins);
            await InitService(TestCatalogFactory.Catalog(product));

            LogAssert.ignoreFailingMessages = true;   // expects a LogError for the missing handler
            _svc.InitiatePurchase("c.coins");
            await UniTask.Yield();
            await UniTask.Yield();
            LogAssert.ignoreFailingMessages = false;

            Assert.That(_backend.FinalizedTransactions.Count, Is.EqualTo(0));
        });

        [UnityTest]
        public System.Collections.IEnumerator ThrowingHandler_DoesNotFinalize() => UniTask.ToCoroutine(async () =>
        {
            var coins = TestCatalogFactory.CoinReward(1000);
            var product = TestCatalogFactory.Product("c.coins", PurchaseProductType.Consumable, coins);
            await InitService(TestCatalogFactory.Catalog(product));
            _svc.RegisterRewardHandler<CoinReward>((r, _) => throw new System.Exception("grant boom"));

            LogAssert.ignoreFailingMessages = true;   // expects a LogError for the thrown handler
            _svc.InitiatePurchase("c.coins");
            await UniTask.Yield();
            await UniTask.Yield();
            LogAssert.ignoreFailingMessages = false;

            Assert.That(_backend.FinalizedTransactions.Count, Is.EqualTo(0));
        });

        [UnityTest]
        public System.Collections.IEnumerator FailedPurchase_DoesNotFinalize() => UniTask.ToCoroutine(async () =>
        {
            var coins = TestCatalogFactory.CoinReward(1000);
            var product = TestCatalogFactory.Product("c.coins", PurchaseProductType.Consumable, coins);
            await InitService(TestCatalogFactory.Catalog(product));

            _backend.ForceNextFailureWithReason(PurchaseFailureReason.PaymentDeclined);
            _svc.InitiatePurchase("c.coins");
            await UniTask.Yield();
            await UniTask.Yield();

            Assert.That(_backend.FinalizedTransactions.Count, Is.EqualTo(0));
        });

        [UnityTest]
        public System.Collections.IEnumerator InvalidReceipt_GrantsNothingAndDoesNotFinalize() => UniTask.ToCoroutine(async () =>
        {
            var coins = TestCatalogFactory.CoinReward(1000);
            var product = TestCatalogFactory.Product("c.coins", PurchaseProductType.Consumable, coins);

            // Validator rejects the receipt: reward must not be granted and the order must NOT be
            // finalized, so the store can re-deliver it.
            _svc = PurchasingService.CreateForTests(TestCatalogFactory.Catalog(product), _backend, _ent, _proc, new RejectingReceiptValidator());
            await UniTask.Yield();

            int coinsGranted = 0;
            _svc.RegisterRewardHandler<CoinReward>((r, _) => coinsGranted += r.Amount);

            _svc.InitiatePurchase("c.coins");
            await UniTask.Yield();
            await UniTask.Yield();

            Assert.That(coinsGranted, Is.EqualTo(0));
            Assert.That(_backend.FinalizedTransactions.Count, Is.EqualTo(0));
        });

        private class RejectingReceiptValidator : IReceiptValidator
        {
            public UniTask<bool> ValidateAsync(PurchaseReceipt receipt) => UniTask.FromResult(false);
        }

        [UnityTest]
        public System.Collections.IEnumerator EntitlementReward_AutoHandled_NoRegistrationRequired() => UniTask.ToCoroutine(async () =>
        {
            var ent = TestCatalogFactory.EntitlementReward("vip");
            var product = TestCatalogFactory.Product("c.vip", PurchaseProductType.NonConsumable, ent);
            await InitService(TestCatalogFactory.Catalog(product));

            // No RegisterRewardHandler<EntitlementReward> call.
            _svc.InitiatePurchase("c.vip");
            await UniTask.Yield();
            await UniTask.Yield();

            Assert.That(_ent.Has("vip"), Is.True);
        });
    }
}
