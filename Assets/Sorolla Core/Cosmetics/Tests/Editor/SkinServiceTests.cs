using System;
using System.Collections.Generic;
using NUnit.Framework;
using Sorolla.Cosmetics;

namespace Sorolla.Cosmetics.Tests
{
    public class SkinServiceTests
    {
        private Dictionary<string, string> _store;

        private string Load(string key, string def) => _store.TryGetValue(key, out var v) ? v : def;
        private void Save(string key, string value) => _store[key] = value;

        [SetUp]
        public void Setup() => _store = new Dictionary<string, string>();

        // ids in catalog order: two default-unlocked, one locked.
        private SkinService NewService() => new SkinService(
            new[] { "default_a", "default_b", "locked_c" },
            new[] { "default_a", "default_b" },
            Load, Save);

        [Test]
        public void NewPlayer_DefaultSkinsUnlocked_LockedSkinLocked()
        {
            var s = NewService();
            Assert.IsTrue(s.IsUnlocked("default_a"));
            Assert.IsTrue(s.IsUnlocked("default_b"));
            Assert.IsFalse(s.IsUnlocked("locked_c"));
        }

        [Test]
        public void NewPlayer_SelectsFirstDefaultInCatalogOrder()
        {
            var s = NewService();
            Assert.AreEqual("default_a", s.SelectedSkinId);
        }

        [Test]
        public void Unlock_MakesSkinUnlocked_AndFiresOnChanged()
        {
            var s = NewService();
            int changed = 0; s.OnChanged += () => changed++;
            s.Unlock("locked_c");
            Assert.IsTrue(s.IsUnlocked("locked_c"));
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void Select_LockedSkin_ReturnsFalse_AndDoesNotChangeSelection()
        {
            var s = NewService();
            int changed = 0; s.OnChanged += () => changed++;
            bool ok = s.Select("locked_c");
            Assert.IsFalse(ok);
            Assert.AreEqual("default_a", s.SelectedSkinId);
            Assert.AreEqual(0, changed);
        }

        [Test]
        public void Select_UnlockedSkin_UpdatesSelection_AndFiresOnChanged()
        {
            var s = NewService();
            int changed = 0; s.OnChanged += () => changed++;
            bool ok = s.Select("default_b");
            Assert.IsTrue(ok);
            Assert.AreEqual("default_b", s.SelectedSkinId);
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void Unlock_PersistsAcrossReconstruction()
        {
            NewService().Unlock("locked_c");
            var reloaded = NewService(); // same _store
            Assert.IsTrue(reloaded.IsUnlocked("locked_c"));
        }

        [Test]
        public void Selection_PersistsAcrossReconstruction()
        {
            NewService().Select("default_b");
            var reloaded = NewService();
            Assert.AreEqual("default_b", reloaded.SelectedSkinId);
        }

        [Test]
        public void UnlockThenSelect_AllowsSelectingPreviouslyLockedSkin()
        {
            var s = NewService();
            Assert.IsFalse(s.Select("locked_c"));
            s.Unlock("locked_c");
            Assert.IsTrue(s.Select("locked_c"));
            Assert.AreEqual("locked_c", s.SelectedSkinId);
        }
    }
}
