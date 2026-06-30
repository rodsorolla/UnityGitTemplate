using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Sorolla.Events.Tests
{
    public class EventSchedulerTests
    {
        // 2026-05-11 is a Monday — use it as the anchor for weekday-based tests.
        private static readonly DateTime Mon = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc);

        private static EventDefinition Def(string id, DayOfWeek start, DayOfWeek end) => new EventDefinition
        {
            EventId = id,
            StartDayOfWeek = start,
            EndDayOfWeek = end,
        };

        [Test]
        public void GetActive_ReturnsNull_OnEmptyCatalog()
        {
            Assert.IsNull(EventScheduler.GetActive(new List<EventDefinition>(), Mon));
        }

        [Test]
        public void GetActive_ReturnsNull_WhenNoEventCoversToday()
        {
            var cat = new List<EventDefinition> { Def("a", DayOfWeek.Friday, DayOfWeek.Saturday) };
            // Monday is outside Fri-Sat.
            Assert.IsNull(EventScheduler.GetActive(cat, Mon));
        }

        [Test]
        public void GetActive_ReturnsEventA_InsideAWindow()
        {
            var cat = new List<EventDefinition>
            {
                Def("a", DayOfWeek.Monday, DayOfWeek.Wednesday),   // Mon, Tue, Wed
                Def("b", DayOfWeek.Thursday, DayOfWeek.Sunday),    // Thu, Fri, Sat, Sun
            };
            var tue = Mon.AddDays(1);
            Assert.AreEqual("a", EventScheduler.GetActive(cat, tue).EventId);
        }

        [Test]
        public void GetActive_ReturnsEventB_AfterCutover()
        {
            var cat = new List<EventDefinition>
            {
                Def("a", DayOfWeek.Monday, DayOfWeek.Wednesday),
                Def("b", DayOfWeek.Thursday, DayOfWeek.Sunday),
            };
            var thu = Mon.AddDays(3);
            Assert.AreEqual("b", EventScheduler.GetActive(cat, thu).EventId);
        }

        [Test]
        public void GetActive_WrapAround_IncludesSunday()
        {
            // Sat → Mon wraps Sunday.
            var cat = new List<EventDefinition> { Def("a", DayOfWeek.Saturday, DayOfWeek.Monday) };
            var sun = Mon.AddDays(-1);            // Sunday
            var nextMon = Mon.AddDays(7);         // following Monday
            var sat = Mon.AddDays(-2);            // previous Saturday
            Assert.AreEqual("a", EventScheduler.GetActive(cat, sun)?.EventId);
            Assert.AreEqual("a", EventScheduler.GetActive(cat, nextMon)?.EventId);
            Assert.AreEqual("a", EventScheduler.GetActive(cat, sat)?.EventId);
            Assert.IsNull(EventScheduler.GetActive(cat, Mon.AddDays(1))); // Tuesday is out of window
        }

        [Test]
        public void GetActive_OverlapPicksLaterPeriodStart()
        {
            var cat = new List<EventDefinition>
            {
                Def("a", DayOfWeek.Sunday, DayOfWeek.Saturday), // every day; period start = Sunday
                Def("b", DayOfWeek.Tuesday, DayOfWeek.Saturday), // period start = Tuesday this week
            };
            var wed = Mon.AddDays(2);
            // Both cover Wednesday; b's period started later (Tue > prev Sun) → b wins.
            Assert.AreEqual("b", EventScheduler.GetActive(cat, wed).EventId);
        }

        [Test]
        public void TimeUntilEnd_IsZero_WhenOutsideWindow()
        {
            var def = Def("a", DayOfWeek.Monday, DayOfWeek.Wednesday);
            var thu = Mon.AddDays(3);
            Assert.AreEqual(TimeSpan.Zero, EventScheduler.TimeUntilEnd(def, thu));
        }

        [Test]
        public void TimeUntilEnd_ReturnsRemaining_WhenInside()
        {
            var def = Def("a", DayOfWeek.Monday, DayOfWeek.Wednesday);
            // Wednesday 22:00 UTC → end at Thursday 00:00 UTC = 2 hours.
            var now = Mon.AddDays(2).AddHours(22);
            Assert.AreEqual(TimeSpan.FromHours(2), EventScheduler.TimeUntilEnd(def, now));
        }

        [Test]
        public void TimeUntilEnd_SpansFullEndDay()
        {
            var def = Def("a", DayOfWeek.Monday, DayOfWeek.Wednesday);
            // Tuesday 00:00 UTC → end at Thursday 00:00 UTC = 48 hours.
            var tue = Mon.AddDays(1);
            Assert.AreEqual(TimeSpan.FromHours(48), EventScheduler.TimeUntilEnd(def, tue));
        }

        [Test]
        public void TimeUntilNextStart_FindsSoonestFuture()
        {
            var cat = new List<EventDefinition>
            {
                Def("a", DayOfWeek.Monday, DayOfWeek.Wednesday),
                Def("b", DayOfWeek.Thursday, DayOfWeek.Sunday),
            };
            // Wednesday 23:00 UTC → next start of "b" is Thursday 00:00 UTC = 1 hour.
            var now = Mon.AddDays(2).AddHours(23);
            Assert.AreEqual(TimeSpan.FromHours(1), EventScheduler.TimeUntilNextStart(cat, now));
        }

        // --- WinStreak archetype ---

        private static EventDefinition WinStreakDef(
            string id,
            int unlockLevel,
            params (int threshold, string[] boosters)[] tiers)
        {
            var def = new EventDefinition
            {
                EventId = id,
                EventType = "win_streak",
                UnlockLevel = unlockLevel,
            };
            foreach (var t in tiers)
            {
                def.WinStreakTiers.Add(new WinStreakTier
                {
                    ThresholdWins = t.threshold,
                    BoosterIds = new List<string>(t.boosters),
                });
            }
            return def;
        }

        [Test]
        public void GetActiveWinStreak_ReturnsNull_OnEmptyCatalog()
        {
            Assert.IsNull(EventScheduler.GetActiveWinStreak(
                new List<EventDefinition>(), playerLevel: 50));
        }

        [Test]
        public void GetActiveWinStreak_ReturnsNull_WhenPlayerBelowUnlockLevel()
        {
            var cat = new List<EventDefinition>
            {
                WinStreakDef("ws", unlockLevel: 12, (3, new[] { "magnet" })),
            };
            Assert.IsNull(EventScheduler.GetActiveWinStreak(cat, playerLevel: 11));
        }

        [Test]
        public void GetActiveWinStreak_ReturnsDef_WhenAtOrAboveUnlock()
        {
            var cat = new List<EventDefinition>
            {
                WinStreakDef("ws", unlockLevel: 12, (3, new[] { "magnet" })),
            };
            Assert.AreEqual("ws", EventScheduler.GetActiveWinStreak(cat, playerLevel: 12).EventId);
            Assert.AreEqual("ws", EventScheduler.GetActiveWinStreak(cat, playerLevel: 99).EventId);
        }

        [Test]
        public void GetActiveWinStreak_IgnoresTreasureHuntEntries()
        {
            var cat = new List<EventDefinition>
            {
                Def("th", DayOfWeek.Monday, DayOfWeek.Sunday),  // treasure hunt (EventType blank)
                WinStreakDef("ws", unlockLevel: 12, (3, new[] { "magnet" })),
            };
            Assert.AreEqual("ws", EventScheduler.GetActiveWinStreak(cat, playerLevel: 20).EventId);
        }

        [Test]
        public void ResolveTierIndex_ReturnsMinusOne_WhenBelowFirstThreshold()
        {
            var def = WinStreakDef("ws", 0,
                (3, new[] { "magnet" }),
                (6, new[] { "magnet", "speed_up" }),
                (9, new[] { "magnet", "speed_up", "start_big" }));
            Assert.AreEqual(-1, EventScheduler.ResolveTierIndex(def, streak: 0));
            Assert.AreEqual(-1, EventScheduler.ResolveTierIndex(def, streak: 2));
        }

        [Test]
        public void ResolveTierIndex_PromotesAtEachThreshold()
        {
            var def = WinStreakDef("ws", 0,
                (3, new[] { "magnet" }),
                (6, new[] { "magnet", "speed_up" }),
                (9, new[] { "magnet", "speed_up", "start_big" }));
            Assert.AreEqual(0, EventScheduler.ResolveTierIndex(def, streak: 3));
            Assert.AreEqual(0, EventScheduler.ResolveTierIndex(def, streak: 5));
            Assert.AreEqual(1, EventScheduler.ResolveTierIndex(def, streak: 6));
            Assert.AreEqual(1, EventScheduler.ResolveTierIndex(def, streak: 8));
            Assert.AreEqual(2, EventScheduler.ResolveTierIndex(def, streak: 9));
            Assert.AreEqual(2, EventScheduler.ResolveTierIndex(def, streak: 999));
        }

        [Test]
        public void ResolveTierIndex_ReturnsMinusOne_OnNullOrEmptyTiers()
        {
            var def = new EventDefinition { EventId = "x", EventType = "win_streak" };
            Assert.AreEqual(-1, EventScheduler.ResolveTierIndex(def, streak: 100));
        }
    }
}
