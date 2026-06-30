using NUnit.Framework;
using UnityEngine;
using Sorolla.Events;
using Sorolla.Tournaments;

namespace Sorolla.Tournaments.Tests
{
    public class TournamentConfigTests
    {
        [Test]
        public void PodiumReward_MapsByRank()
        {
            var t = new TierDefinition
            {
                podiumRank1 = new[] { new EventReward { ItemType = "coins", Amount = 100 } }
            };
            Assert.AreEqual(100, t.PodiumReward(1)[0].Amount);
            Assert.AreEqual(0, t.PodiumReward(4).Length);
        }

        [Test]
        public void ToData_IsValid_WhenPopulated()
        {
            var so = ScriptableObject.CreateInstance<TournamentConfig>();
            so.tiers.Add(new TierDefinition());
            so.botNames.Add("Alpha");
            Assert.IsTrue(so.ToData().IsValid);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ToData_IsInvalid_WhenEmpty()
        {
            var so = ScriptableObject.CreateInstance<TournamentConfig>();
            Assert.IsFalse(so.ToData().IsValid);
            Object.DestroyImmediate(so);
        }
    }
}
