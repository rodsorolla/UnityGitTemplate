using NUnit.Framework;
using Sorolla.Tournaments;

namespace Sorolla.Tournaments.Tests
{
    public class StableHashTests
    {
        [Test] public void Combine_IsDeterministic()
            => Assert.AreEqual(StableHash.Combine(1, 2, 3), StableHash.Combine(1, 2, 3));

        [Test] public void Combine_DiffersByInput()
            => Assert.AreNotEqual(StableHash.Combine(1, 2, 3), StableHash.Combine(1, 2, 4));

        [Test] public void OfString_IsDeterministic()
            => Assert.AreEqual(StableHash.OfString("abc"), StableHash.OfString("abc"));

        [Test] public void Range_StaysWithinBounds()
        {
            for (uint h = 0; h < 64; h++)
            {
                int v = StableHash.RangeInclusive(h * 2654435761u, 5, 10);
                Assert.GreaterOrEqual(v, 5);
                Assert.LessOrEqual(v, 10);
            }
        }

        [Test] public void Range_MinEqualsMax_ReturnsMin()
            => Assert.AreEqual(7, StableHash.RangeInclusive(123u, 7, 7));
    }
}
