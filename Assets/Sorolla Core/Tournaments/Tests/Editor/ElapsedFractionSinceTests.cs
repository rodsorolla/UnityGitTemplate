using System;
using NUnit.Framework;
using Sorolla.Tournaments;

namespace Sorolla.Tournaments.Tests
{
    public class ElapsedFractionSinceTests
    {
        // Join at/before week start reproduces the calendar ElapsedFraction exactly.
        [Test] public void OnTimeJoin_MatchesElapsedFraction_MidWeek()
        {
            DateTime start = TournamentWeek.WeekStartUtc(3);
            DateTime now = start.AddDays(3.5);
            Assert.AreEqual(
                TournamentWeek.ElapsedFraction(now, 3),
                TournamentWeek.ElapsedFractionSince(now, start, 3),
                1e-9);
        }

        // Join before the week start clamps the anchor to week start (still on-time behavior).
        [Test] public void JoinBeforeWeekStart_ClampsToWeekStart()
        {
            DateTime start = TournamentWeek.WeekStartUtc(3);
            DateTime now = start.AddDays(3.5);
            Assert.AreEqual(
                TournamentWeek.ElapsedFraction(now, 3),
                TournamentWeek.ElapsedFractionSince(now, start.AddDays(-2), 3),
                1e-9);
        }

        // At the join instant, the fraction is 0 (bots read ~0).
        [Test] public void AtJoinInstant_IsZero()
        {
            DateTime join = TournamentWeek.WeekStartUtc(3).AddDays(4); // joined Friday-ish
            Assert.AreEqual(0.0, TournamentWeek.ElapsedFractionSince(join, join, 3), 1e-9);
        }

        // At week end, the fraction is 1 (bots reach full target).
        [Test] public void AtWeekEnd_IsOne()
        {
            DateTime join = TournamentWeek.WeekStartUtc(3).AddDays(4);
            DateTime end = TournamentWeek.WeekEndUtc(3);
            Assert.AreEqual(1.0, TournamentWeek.ElapsedFractionSince(end, join, 3), 1e-9);
        }

        // Halfway between a mid-week join and week end is 0.5, and it is monotonic.
        [Test] public void HalfwayBetweenJoinAndEnd_IsHalf()
        {
            int week = 3;
            DateTime join = TournamentWeek.WeekStartUtc(week).AddDays(3); // 4 days remain
            DateTime mid = join.AddDays(2);                              // halfway to end
            double f = TournamentWeek.ElapsedFractionSince(mid, join, week);
            Assert.AreEqual(0.5, f, 1e-9);
        }

        // Before the anchor clamps to 0; after week end clamps to 1.
        [Test] public void OutOfRange_Clamps()
        {
            int week = 3;
            DateTime join = TournamentWeek.WeekStartUtc(week).AddDays(3);
            Assert.AreEqual(0.0, TournamentWeek.ElapsedFractionSince(join.AddHours(-1), join, week), 1e-9);
            Assert.AreEqual(1.0, TournamentWeek.ElapsedFractionSince(TournamentWeek.WeekEndUtc(week).AddDays(1), join, week), 1e-9);
        }

        // A join exactly at week end yields 1 with no divide-by-zero.
        [Test] public void JoinAtWeekEnd_IsOne_NoDivideByZero()
        {
            int week = 3;
            DateTime end = TournamentWeek.WeekEndUtc(week);
            Assert.AreEqual(1.0, TournamentWeek.ElapsedFractionSince(end, end, week), 1e-9);
        }
    }
}
