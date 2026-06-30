using System;
using NUnit.Framework;
using Sorolla.Tournaments;

namespace Sorolla.Tournaments.Tests
{
    public class TournamentWeekTests
    {
        static readonly DateTime Epoch = new DateTime(1970, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        [Test] public void WeekIndex_AtEpoch_IsZero()
            => Assert.AreEqual(0, TournamentWeek.WeekIndex(Epoch));

        [Test] public void WeekIndex_OneWeekLater_IsOne()
            => Assert.AreEqual(1, TournamentWeek.WeekIndex(Epoch.AddDays(7)));

        [Test] public void WeekIndex_JustBeforeBoundary_StaysSame()
            => Assert.AreEqual(0, TournamentWeek.WeekIndex(Epoch.AddDays(6).AddHours(23)));

        [Test] public void WeekStart_MatchesEpochMultiple()
            => Assert.AreEqual(Epoch.AddDays(14), TournamentWeek.WeekStartUtc(2));

        [Test] public void ElapsedFraction_AtStart_IsZero()
            => Assert.AreEqual(0.0, TournamentWeek.ElapsedFraction(TournamentWeek.WeekStartUtc(3), 3), 1e-9);

        [Test] public void ElapsedFraction_MidWeek_IsHalf()
            => Assert.AreEqual(0.5, TournamentWeek.ElapsedFraction(TournamentWeek.WeekStartUtc(3).AddDays(3.5), 3), 1e-9);

        [Test] public void ElapsedFraction_PastEnd_ClampsToOne()
            => Assert.AreEqual(1.0, TournamentWeek.ElapsedFraction(TournamentWeek.WeekStartUtc(3).AddDays(10), 3), 1e-9);
    }
}
