using System.IO;
using NUnit.Framework;
using Sorolla.PersistentData;
using Sorolla.Tournaments;

namespace Sorolla.Tournaments.Tests
{
    public class TournamentStateTests
    {
        const string TestFile = "tournament_test";

        [SetUp] public void SetUp() => SaveSystem.Initialize();

        [TearDown]
        public void TearDown()
        {
            var path = SaveSystem.GetFilePath(TestFile);
            if (File.Exists(path)) File.Delete(path);
        }

        [Test]
        public void SaveLoad_RoundTrips_WithPendingResult()
        {
            var s = new TournamentState
            {
                CurrentTierIndex = 2,
                ActiveWeekIndex = 42,
                PlayerTrophies = 7,
                PendingResult = new PendingResult
                {
                    WeekIndex = 41, TierIndex = 2, FinalRank = 3,
                    Outcome = TournamentOutcome.Promoted, Claimed = false
                }
            };

            SaveSystem.Save(s, TestFile);
            var loaded = SaveSystem.Load<TournamentState>(TestFile);

            Assert.AreEqual(2, loaded.CurrentTierIndex);
            Assert.AreEqual(42, loaded.ActiveWeekIndex);
            Assert.AreEqual(7, loaded.PlayerTrophies);
            Assert.IsNotNull(loaded.PendingResult);
            Assert.AreEqual(3, loaded.PendingResult.FinalRank);
            Assert.AreEqual(TournamentOutcome.Promoted, loaded.PendingResult.Outcome);
        }
    }
}
