using NUnit.Framework;
using UnityEngine;
using Sorolla.Profile;

namespace Sorolla.Profile.Tests
{
    public class ProfileLogicTests
    {
        ProfileCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<ProfileCatalog>();
            _catalog.avatars.Add(new ProfileCatalog.AvatarEntry { id = "a1" });
            _catalog.flags.Add(new ProfileCatalog.FlagEntry { countryCode = "US" });
            _catalog.defaultCountryCode = "US";
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(_catalog);

        [Test]
        public void SeedDefaults_UsesDeviceRegion_WhenInCatalog()
        {
            var d = ProfileLogic.SeedDefaults(_catalog, "US", 1234);
            Assert.AreEqual("US", d.CountryCode);
            Assert.AreEqual("Player1234", d.DisplayName);
            Assert.AreEqual("a1", d.AvatarId);
            Assert.IsFalse(d.IsCustomName);
        }

        [Test]
        public void SeedDefaults_FallsBackToDefault_WhenRegionMissing()
        {
            var d = ProfileLogic.SeedDefaults(_catalog, "ZZ", 1);
            Assert.AreEqual("US", d.CountryCode);
        }

        [Test]
        public void ResolveAvatarId_FallsBack_WhenStale()
            => Assert.AreEqual("a1", ProfileLogic.ResolveAvatarId(_catalog, "gone"));

        [Test]
        public void ResolveAvatarId_Keeps_WhenValid()
            => Assert.AreEqual("a1", ProfileLogic.ResolveAvatarId(_catalog, "a1"));

        [Test]
        public void ResolveCountryCode_FallsBack_WhenStale()
            => Assert.AreEqual("US", ProfileLogic.ResolveCountryCode(_catalog, "ZZ"));

        [Test]
        public void ResolveCountryCode_Keeps_WhenValid()
            => Assert.AreEqual("US", ProfileLogic.ResolveCountryCode(_catalog, "US"));
    }
}
