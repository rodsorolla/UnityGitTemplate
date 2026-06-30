using System.IO;
using NUnit.Framework;
using Sorolla.PersistentData;
using Sorolla.Profile;

namespace Sorolla.Profile.Tests
{
    public class PlayerProfileDataTests
    {
        const string TestFile = "profile_test";

        [SetUp] public void SetUp() => SaveSystem.Initialize();

        [TearDown]
        public void TearDown()
        {
            var path = SaveSystem.GetFilePath(TestFile);
            if (File.Exists(path)) File.Delete(path);
        }

        [Test]
        public void SaveThenLoad_RoundTripsAllFields()
        {
            var data = new PlayerProfileData
            {
                DisplayName = "Snakey",
                AvatarId = "avatar_03",
                CountryCode = "US",
                IsCustomName = true
            };

            SaveSystem.Save(data, TestFile);
            var loaded = SaveSystem.Load<PlayerProfileData>(TestFile);

            Assert.AreEqual("Snakey", loaded.DisplayName);
            Assert.AreEqual("avatar_03", loaded.AvatarId);
            Assert.AreEqual("US", loaded.CountryCode);
            Assert.IsTrue(loaded.IsCustomName);
        }
    }
}
