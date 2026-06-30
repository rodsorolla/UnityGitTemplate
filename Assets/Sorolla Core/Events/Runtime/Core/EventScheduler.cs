using System;
using System.Collections.Generic;

namespace Sorolla.Events
{
    /// <summary>
    /// Pure-logic resolver: given a catalog and a UTC instant, return the active
    /// event (or null). Each <see cref="EventDefinition"/> recurs weekly between
    /// its <see cref="EventDefinition.StartDayOfWeek"/> (00:00 UTC, inclusive)
    /// and the 00:00 UTC midnight after its <see cref="EventDefinition.EndDayOfWeek"/>
    /// (inclusive of the end day). When multiple windows overlap, the one whose
    /// current period started later wins — this is the "Replaced" path.
    /// </summary>
    public static class EventScheduler
    {
        /// <summary>
        /// Returns the active event for <paramref name="utcNow"/>, or null when
        /// none has its weekday window open.
        /// </summary>
        public static EventDefinition GetActive(IReadOnlyList<EventDefinition> catalog, DateTime utcNow)
        {
            if (catalog == null || catalog.Count == 0) return null;

            EventDefinition winner = null;
            DateTime winnerPeriodStart = DateTime.MinValue;

            for (int i = 0; i < catalog.Count; i++)
            {
                var def = catalog[i];
                if (def == null) continue;
                if (!IsActiveOn(def, utcNow)) continue;
                var periodStart = CurrentPeriodStartUtc(def, utcNow);
                if (winner == null || periodStart > winnerPeriodStart)
                {
                    winner = def;
                    winnerPeriodStart = periodStart;
                }
            }

            return winner;
        }

        /// <summary>
        /// Time remaining until the end of <paramref name="def"/>'s current
        /// active period (midnight UTC after EndDayOfWeek), clamped to zero when
        /// the window is closed.
        /// </summary>
        public static TimeSpan TimeUntilEnd(EventDefinition def, DateTime utcNow)
        {
            if (def == null) return TimeSpan.Zero;
            if (!IsActiveOn(def, utcNow)) return TimeSpan.Zero;
            var remaining = CurrentPeriodEndUtc(def, utcNow) - utcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        /// <summary>
        /// Returns the soonest future event start across the catalog (next
        /// StartDayOfWeek occurrence at 00:00 UTC strictly after
        /// <paramref name="utcNow"/>), or TimeSpan.Zero when the catalog is empty.
        /// </summary>
        public static TimeSpan TimeUntilNextStart(IReadOnlyList<EventDefinition> catalog, DateTime utcNow)
        {
            if (catalog == null || catalog.Count == 0) return TimeSpan.Zero;
            TimeSpan best = TimeSpan.Zero;
            bool any = false;
            for (int i = 0; i < catalog.Count; i++)
            {
                var def = catalog[i];
                if (def == null) continue;
                var delta = NextStartUtc(def, utcNow) - utcNow;
                if (delta <= TimeSpan.Zero) continue;
                if (!any || delta < best) { best = delta; any = true; }
            }
            return any ? best : TimeSpan.Zero;
        }

        /// <summary>
        /// True when the event's weekday window contains <paramref name="utcNow"/>.
        /// Supports wrap-around windows where <c>EndDayOfWeek &lt; StartDayOfWeek</c>.
        /// </summary>
        public static bool IsActiveOn(EventDefinition def, DateTime utcNow)
        {
            if (def == null) return false;
            int today = (int)utcNow.DayOfWeek;
            int s = (int)def.StartDayOfWeek;
            int e = (int)def.EndDayOfWeek;
            return s <= e ? (today >= s && today <= e) : (today >= s || today <= e);
        }

        // ---- Internals ----

        /// <summary>Midnight UTC of the StartDayOfWeek of the current active period.</summary>
        private static DateTime CurrentPeriodStartUtc(EventDefinition def, DateTime utcNow)
        {
            var date = DateTime.SpecifyKind(utcNow.Date, DateTimeKind.Utc);
            int s = (int)def.StartDayOfWeek;
            for (int i = 0; i < 7; i++)
            {
                if ((int)date.DayOfWeek == s) return date;
                date = date.AddDays(-1);
            }
            return date;
        }

        /// <summary>Midnight UTC of the day after EndDayOfWeek for the current active period.</summary>
        private static DateTime CurrentPeriodEndUtc(EventDefinition def, DateTime utcNow)
        {
            var date = DateTime.SpecifyKind(utcNow.Date, DateTimeKind.Utc);
            int e = (int)def.EndDayOfWeek;
            for (int i = 0; i < 7; i++)
            {
                if ((int)date.DayOfWeek == e) return date.AddDays(1);
                date = date.AddDays(1);
            }
            return date;
        }

        /// <summary>Midnight UTC of the next StartDayOfWeek occurrence strictly after <paramref name="utcNow"/>.</summary>
        private static DateTime NextStartUtc(EventDefinition def, DateTime utcNow)
        {
            var date = DateTime.SpecifyKind(utcNow.Date, DateTimeKind.Utc).AddDays(1);
            int s = (int)def.StartDayOfWeek;
            for (int i = 0; i < 7; i++)
            {
                if ((int)date.DayOfWeek == s) return date;
                date = date.AddDays(1);
            }
            return date;
        }

        /// <summary>
        /// Returns the active WinStreak event from the catalog, or null if none
        /// is present or the player is below its unlock level. Time-of-day is
        /// irrelevant — WinStreak events are always active once unlocked.
        /// </summary>
        public static EventDefinition GetActiveWinStreak(
            IReadOnlyList<EventDefinition> catalog,
            int playerLevel)
        {
            if (catalog == null) return null;
            for (int i = 0; i < catalog.Count; i++)
            {
                var def = catalog[i];
                if (def == null) continue;
                if (def.EventType != "win_streak") continue;

                int unlock = def.UnlockLevel >= 0 ? def.UnlockLevel : EventConfigKeys.DefaultUnlockLevelValue;
                if (playerLevel < unlock) continue;

                return def;
            }
            return null;
        }

        /// <summary>
        /// Returns the 0-based index of the highest tier the given streak satisfies,
        /// or -1 if the streak is below the first tier's threshold (or tiers are absent).
        /// </summary>
        public static int ResolveTierIndex(EventDefinition def, int streak)
        {
            if (def == null) return -1;
            var tiers = def.WinStreakTiers;
            if (tiers == null || tiers.Count == 0) return -1;
            int result = -1;
            for (int i = 0; i < tiers.Count; i++)
            {
                if (streak >= tiers[i].ThresholdWins) result = i;
            }
            return result;
        }
    }
}
