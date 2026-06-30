using NUnit.Framework;
using HungrySnake.GoldenEgg;

namespace HungrySnake.GoldenEgg.Tests
{
    public class RemoteGoldenEggCatalogTests
    {
        private const string ValidJson = @"{
            ""events"": [{
                ""eventId"": ""golden_egg_v1"",
                ""displayName"": ""Golden Egg"",
                ""description"": ""Win in a row to receive free boosters."",
                ""unlockLevel"": 12,
                ""resetOnQuit"": true,
                ""tiers"": [
                    { ""thresholdWins"": 3, ""boosterIds"": [""magnet""] },
                    { ""thresholdWins"": 6, ""boosterIds"": [""magnet"", ""speed_up""] },
                    { ""thresholdWins"": 9, ""boosterIds"": [""magnet"", ""speed_up"", ""start_big""] }
                ]
            }]
        }";

        [Test]
        public void Parse_ReturnsNull_OnNullOrEmpty()
        {
            Assert.IsNull(RemoteGoldenEggCatalog.Parse(null));
            Assert.IsNull(RemoteGoldenEggCatalog.Parse(""));
            Assert.IsNull(RemoteGoldenEggCatalog.Parse("   "));
        }

        [Test]
        public void Parse_ReturnsNull_OnMalformedJson()
        {
            Assert.IsNull(RemoteGoldenEggCatalog.Parse("{ this is not valid json"));
        }

        [Test]
        public void Parse_ProducesOneEntryWithThreeTiers()
        {
            var dto = RemoteGoldenEggCatalog.Parse(ValidJson);
            Assert.IsNotNull(dto);
            Assert.AreEqual(1, dto.events.Count);
            var entry = dto.events[0];
            Assert.AreEqual("golden_egg_v1", entry.eventId);
            Assert.AreEqual(12, entry.unlockLevel);
            Assert.AreEqual(3, entry.tiers.Count);
            Assert.AreEqual(3, entry.tiers[0].thresholdWins);
            Assert.AreEqual(9, entry.tiers[2].thresholdWins);
            Assert.AreEqual(3, entry.tiers[2].boosterIds.Count);
        }

        [Test]
        public void ToDefinitions_MapsAllFields()
        {
            var dto = RemoteGoldenEggCatalog.Parse(ValidJson);
            var defs = dto.ToDefinitions();
            Assert.AreEqual(1, defs.Count);
            var def = defs[0];
            Assert.AreEqual("golden_egg_v1", def.EventId);
            Assert.AreEqual("win_streak", def.EventType);
            Assert.AreEqual(12, def.UnlockLevel);
            Assert.AreEqual(3, def.WinStreakTiers.Count);
            Assert.AreEqual(9, def.WinStreakTiers[2].ThresholdWins);
            CollectionAssert.AreEqual(
                new[] { "magnet", "speed_up", "start_big" },
                def.WinStreakTiers[2].BoosterIds);
        }
    }
}
