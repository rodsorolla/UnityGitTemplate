using NUnit.Framework;
using UnityEngine;
using Sorolla.Profile;

namespace Sorolla.Profile.Tests
{
    public class ProfileCatalogTests
    {
        ProfileCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<ProfileCatalog>();
            _catalog.avatars.Add(new ProfileCatalog.AvatarEntry { id = "a1" });
            _catalog.avatars.Add(new ProfileCatalog.AvatarEntry { id = "a2" });
            _catalog.flags.Add(new ProfileCatalog.FlagEntry { countryCode = "US" });
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(_catalog);

        [Test] public void HasAvatar_TrueForExisting() => Assert.IsTrue(_catalog.HasAvatar("a2"));
        [Test] public void HasAvatar_FalseForMissing() => Assert.IsFalse(_catalog.HasAvatar("zzz"));
        [Test] public void HasFlag_TrueForExisting() => Assert.IsTrue(_catalog.HasFlag("US"));
        [Test] public void HasFlag_FalseForMissing() => Assert.IsFalse(_catalog.HasFlag("ZZ"));
        [Test] public void FirstAvatarId_ReturnsFirst() => Assert.AreEqual("a1", _catalog.FirstAvatarId);
    }
}
