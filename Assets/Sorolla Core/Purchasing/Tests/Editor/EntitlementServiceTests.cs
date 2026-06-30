using System.IO;
using NUnit.Framework;
using Sorolla.PersistentData;
using UnityEngine;

namespace Sorolla.Purchasing.Tests
{
    public class EntitlementServiceTests
    {
        private const string TestSaveFile = "entitlements_test";
        private const string LegacySaveFile = "iap_test";

        private EntitlementService _svc;

        [SetUp]
        public void Setup()
        {
            SaveSystem.Delete(TestSaveFile);
            SaveSystem.Delete(LegacySaveFile);
            _svc = EntitlementService.CreateForTests(TestSaveFile, LegacySaveFile);
        }

        [TearDown]
        public void Teardown()
        {
            if (_svc != null)
            {
                Object.DestroyImmediate(_svc.gameObject);
                _svc = null;
            }
            SaveSystem.Delete(TestSaveFile);
            SaveSystem.Delete(LegacySaveFile);
        }

        [Test]
        public void FreshInstall_HasNothing()
        {
            Assert.That(_svc.Has("noads"), Is.False);
        }

        [Test]
        public void Grant_AddsKey_AndPersists()
        {
            int events = 0;
            _svc.OnEntitlementChanged += (_, _) => events++;

            _svc.Grant("noads");

            Assert.That(_svc.Has("noads"), Is.True);
            Assert.That(events, Is.EqualTo(1));

            // Reload via a fresh service to confirm persistence.
            Object.DestroyImmediate(_svc.gameObject);
            _svc = EntitlementService.CreateForTests(TestSaveFile, LegacySaveFile);
            Assert.That(_svc.Has("noads"), Is.True);
        }

        [Test]
        public void Grant_IsIdempotent_NoEventOnRegrant()
        {
            int events = 0;
            _svc.Grant("noads");
            _svc.OnEntitlementChanged += (_, _) => events++;

            _svc.Grant("noads");

            Assert.That(events, Is.EqualTo(0));
        }

        [Test]
        public void Revoke_RemovesKeyAndEmitsEvent()
        {
            _svc.Grant("noads");
            int events = 0;
            _svc.OnEntitlementChanged += (_, _) => events++;

            _svc.Revoke("noads");

            Assert.That(_svc.Has("noads"), Is.False);
            Assert.That(events, Is.EqualTo(1));
        }

        [Test]
        public void LegacyMigration_ReadsHasNoAds_AndDeletesLegacyFile()
        {
            // Write legacy file directly through SaveSystem using a local stub that matches
            // the historical schema { Version: 1, HasNoAds: true }.
            SaveSystem.Save(new LegacyIapStub { HasNoAds = true }, LegacySaveFile);
            Object.DestroyImmediate(_svc.gameObject);

            _svc = EntitlementService.CreateForTests(TestSaveFile, LegacySaveFile);

            Assert.That(_svc.Has("noads"), Is.True);
            Assert.That(SaveSystem.Exists(LegacySaveFile), Is.False);
        }

        [System.Serializable]
        private class LegacyIapStub : ISaveData
        {
            public int Version => 1;
            public bool HasNoAds;
        }
    }
}
