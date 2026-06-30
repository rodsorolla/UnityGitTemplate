using System.Collections.Generic;
using NUnit.Framework;
using Sorolla.Tournaments;

namespace Sorolla.Tournaments.Tests
{
    public class StandingsTests
    {
        static List<int> Repeat(int value, int times)
        {
            var l = new List<int>(times);
            for (int i = 0; i < times; i++) l.Add(value);
            return l;
        }

        [Test]
        public void PlayerTop_IsRank1_Podium_Promoted()
        {
            var bots = Repeat(0, 99);                 // 99 bots at 0
            var r = Standings.Compute(10, bots, 0.20f, 0.20f);
            Assert.AreEqual(1, r.PlayerRank);
            Assert.AreEqual(100, r.Group);
            Assert.AreEqual(20, r.PromoteCount);
            Assert.AreEqual(20, r.DemoteCount);
            Assert.IsTrue(r.PlayerIsPodium);
            Assert.AreEqual(TournamentOutcome.Promoted, r.PlayerOutcome);
        }

        [Test]
        public void PlayerBottom_IsLastRank_Demoted()
        {
            var bots = Repeat(100, 99);               // 99 bots above the player
            var r = Standings.Compute(0, bots, 0.20f, 0.20f);
            Assert.AreEqual(100, r.PlayerRank);
            Assert.AreEqual(TournamentOutcome.Demoted, r.PlayerOutcome);
            Assert.IsFalse(r.PlayerIsPodium);
        }

        [Test]
        public void PlayerMiddle_Stays()
        {
            var bots = new List<int>();
            for (int i = 0; i < 49; i++) bots.Add(100); // 49 above
            for (int i = 0; i < 50; i++) bots.Add(0);   // 50 below
            var r = Standings.Compute(50, bots, 0.20f, 0.20f);
            Assert.AreEqual(50, r.PlayerRank);
            Assert.AreEqual(TournamentOutcome.Stayed, r.PlayerOutcome);
        }

        [Test]
        public void TieKeepsPlayerAhead()
        {
            var bots = Repeat(10, 5);                  // all equal to player
            var r = Standings.Compute(10, bots, 0.20f, 0.20f);
            Assert.AreEqual(1, r.PlayerRank);          // no bot strictly greater
        }
    }
}
