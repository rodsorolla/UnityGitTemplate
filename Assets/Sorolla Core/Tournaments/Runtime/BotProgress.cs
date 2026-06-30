namespace Sorolla.Tournaments
{
    /// A bot's trophy count at a given elapsed fraction of the week.
    /// Always 0 at fraction 0 and exactly the target at fraction 1.
    public static class BotProgress
    {
        // Per-bot pacing exponent range. Low exponents (&lt;1) front-load so some bots populate the
        // board early; high exponents (&gt;1) ramp slowly so other bots start with just 1-2 cups —
        // giving a natural early-week spread. All bots keep climbing the whole week and only reach
        // weeklyTarget at week end: frac^exp is always &lt;1 until frac==1, so no capping/plateau.
        private const double MinExponent = 0.6;
        private const double MaxExponent = 1.3;

        public static int TrophiesAt(int weeklyTarget, int botId, double elapsedFraction)
        {
            if (weeklyTarget <= 0) return 0;
            if (elapsedFraction <= 0) return 0;
            if (elapsedFraction >= 1) return weeklyTarget;

            // Per-bot pacing variation so bots don't move in lockstep.
            uint h = StableHash.Combine(botId, 7);
            double phase = (h % 1000) / 1000.0;          // 0..1
            double exponent = MinExponent + (MaxExponent - MinExponent) * phase;
            double curved = System.Math.Pow(elapsedFraction, exponent);

            int v = (int)System.Math.Round(weeklyTarget * curved);
            if (v < 0) v = 0;
            if (v > weeklyTarget) v = weeklyTarget;
            return v;
        }
    }
}
