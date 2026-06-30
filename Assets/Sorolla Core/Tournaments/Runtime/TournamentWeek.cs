using System;

namespace Sorolla.Tournaments
{
    /// Maps UTC time to integer week indices. Week boundary: Monday 00:00 UTC.
    public static class TournamentWeek
    {
        // 1970-01-05 was a Monday (UTC).
        static readonly DateTime Epoch = new DateTime(1970, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        public static int WeekIndex(DateTime utcNow)
        {
            double days = (DateTime.SpecifyKind(utcNow, DateTimeKind.Utc) - Epoch).TotalDays;
            return (int)Math.Floor(days / 7.0);
        }

        public static DateTime WeekStartUtc(int weekIndex) => Epoch.AddDays(weekIndex * 7);
        public static DateTime WeekEndUtc(int weekIndex) => Epoch.AddDays((weekIndex + 1) * 7);

        public static double ElapsedFraction(DateTime utcNow, int weekIndex)
        {
            double frac = (DateTime.SpecifyKind(utcNow, DateTimeKind.Utc) - WeekStartUtc(weekIndex)).TotalDays / 7.0;
            if (frac < 0) return 0;
            if (frac > 1) return 1;
            return frac;
        }

        /// Elapsed fraction of the week measured from a join anchor instead of the week start:
        /// 0 at the join instant, 1 at the week end. A join at/before the week start reproduces
        /// ElapsedFraction exactly. Used so bots ramp from when the player first sees the board.
        public static double ElapsedFractionSince(DateTime utcNow, DateTime joinUtc, int weekIndex)
        {
            DateTime weekStart = WeekStartUtc(weekIndex);
            DateTime weekEnd = WeekEndUtc(weekIndex);

            DateTime anchor = joinUtc < weekStart ? weekStart : (joinUtc > weekEnd ? weekEnd : joinUtc);

            double total = (weekEnd - anchor).TotalDays;
            if (total <= 0) return 1;
            double frac = (DateTime.SpecifyKind(utcNow, DateTimeKind.Utc) - anchor).TotalDays / total;
            if (frac < 0) return 0;
            if (frac > 1) return 1;
            return frac;
        }
    }
}
