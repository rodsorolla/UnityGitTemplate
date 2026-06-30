using NUnit.Framework;
using Sorolla.Tournaments;

namespace Sorolla.Tournaments.Tests
{
    public class TierTransitionTests
    {
        [Test] public void Promote_Increments()
            => Assert.AreEqual(2, TierTransition.Apply(1, TournamentOutcome.Promoted, 5));

        [Test] public void Promote_ClampsAtTopTier()
            => Assert.AreEqual(4, TierTransition.Apply(4, TournamentOutcome.Promoted, 5));

        [Test] public void Demote_Decrements()
            => Assert.AreEqual(1, TierTransition.Apply(2, TournamentOutcome.Demoted, 5));

        [Test] public void Demote_ClampsAtZero()
            => Assert.AreEqual(0, TierTransition.Apply(0, TournamentOutcome.Demoted, 5));

        [Test] public void Stayed_Unchanged()
            => Assert.AreEqual(3, TierTransition.Apply(3, TournamentOutcome.Stayed, 5));

        [Test] public void Rollover_SameWeek_Holds()
            => Assert.AreEqual(RolloverAction.Hold, RolloverPolicy.Decide(10, 10, 5, false));

        [Test] public void Rollover_Backwards_Holds()
            => Assert.AreEqual(RolloverAction.Hold, RolloverPolicy.Decide(10, 9, 5, false));

        [Test] public void Rollover_RollbackFlag_Holds()
            => Assert.AreEqual(RolloverAction.Hold, RolloverPolicy.Decide(10, 11, 5, true));

        [Test] public void Rollover_FutureWeek_WithTrophies_Finalizes()
            => Assert.AreEqual(RolloverAction.Finalize, RolloverPolicy.Decide(10, 11, 1, false));

        [Test] public void Rollover_FutureWeek_NoTrophies_NoOpAdvances()
            => Assert.AreEqual(RolloverAction.NoOpAdvance, RolloverPolicy.Decide(10, 11, 0, false));
    }
}
