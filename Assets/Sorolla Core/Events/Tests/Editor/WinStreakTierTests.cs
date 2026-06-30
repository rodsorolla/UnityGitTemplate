using System.Collections.Generic;
using NUnit.Framework;

namespace Sorolla.Events.Tests
{
    public class WinStreakTierTests
    {
        [Test]
        public void DefaultConstructor_HasEmptyBoosterList()
        {
            var tier = new WinStreakTier();
            Assert.IsNotNull(tier.BoosterIds);
            Assert.AreEqual(0, tier.BoosterIds.Count);
        }

        [Test]
        public void HoldsThresholdAndBoosters()
        {
            var tier = new WinStreakTier
            {
                ThresholdWins = 6,
                BoosterIds = new List<string> { "magnet", "speed_up" },
            };
            Assert.AreEqual(6, tier.ThresholdWins);
            Assert.AreEqual(2, tier.BoosterIds.Count);
            Assert.AreEqual("magnet", tier.BoosterIds[0]);
            Assert.AreEqual("speed_up", tier.BoosterIds[1]);
        }
    }
}
