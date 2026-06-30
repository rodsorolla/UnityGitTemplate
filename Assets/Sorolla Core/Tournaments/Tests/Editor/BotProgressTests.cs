using NUnit.Framework;
using Sorolla.Tournaments;

namespace Sorolla.Tournaments.Tests
{
    public class BotProgressTests
    {
        [Test] public void AtZero_IsZero()
            => Assert.AreEqual(0, BotProgress.TrophiesAt(40, 3, 0.0));

        [Test] public void AtOne_IsTarget()
            => Assert.AreEqual(40, BotProgress.TrophiesAt(40, 3, 1.0));

        [Test] public void NonNegativeTarget_Zero_ReturnsZero()
            => Assert.AreEqual(0, BotProgress.TrophiesAt(0, 3, 0.5));

        [Test] public void IsMonotonicNonDecreasing()
        {
            int early = BotProgress.TrophiesAt(40, 3, 0.25);
            int late = BotProgress.TrophiesAt(40, 3, 0.75);
            Assert.LessOrEqual(early, late);
        }

        [Test] public void StaysWithinBounds()
        {
            for (double f = 0; f <= 1.0; f += 0.05)
            {
                int v = BotProgress.TrophiesAt(40, 9, f);
                Assert.GreaterOrEqual(v, 0);
                Assert.LessOrEqual(v, 40);
            }
        }
    }
}
