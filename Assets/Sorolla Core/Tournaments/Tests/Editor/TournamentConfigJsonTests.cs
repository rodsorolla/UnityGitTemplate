using NUnit.Framework;
using Sorolla.Tournaments;

namespace Sorolla.Tournaments.Tests
{
    public class TournamentConfigJsonTests
    {
        const string Valid =
            "{\"tiers\":[{\"name\":\"Bronze\",\"groupSize\":50,\"botPaceMin\":5,\"botPaceMax\":30," +
            "\"promotePct\":0.2,\"demotePct\":0.2,\"podium1\":[{\"type\":\"coins\",\"amount\":500}]}]," +
            "\"botNames\":[\"Alpha\",\"Bravo\"]}";

        [Test]
        public void Parse_Valid_ReturnsData()
        {
            var data = TournamentConfigJson.Parse(Valid, out var err);
            Assert.IsNull(err);
            Assert.IsNotNull(data);
            Assert.IsTrue(data.IsValid);
            Assert.AreEqual(1, data.Tiers.Count);
            Assert.AreEqual("Bronze", data.Tiers[0].name);
            Assert.AreEqual(50, data.Tiers[0].groupSize);
            Assert.AreEqual(500, data.Tiers[0].PodiumReward(1)[0].Amount);
            Assert.AreEqual(2, data.BotNames.Count);
        }

        [Test]
        public void Parse_Malformed_ReturnsNullWithError()
        {
            var data = TournamentConfigJson.Parse("{ not json", out var err);
            Assert.IsNull(data);
            Assert.IsNotNull(err);
        }

        [Test]
        public void Parse_Empty_ReturnsNullNoError()
        {
            var data = TournamentConfigJson.Parse("", out var err);
            Assert.IsNull(data);
            Assert.IsNull(err);
        }
    }
}
