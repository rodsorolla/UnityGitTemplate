namespace Sorolla.Tournaments
{
    /// Deterministic, cross-run-stable hashing (FNV-1a). Seeds bot rosters.
    /// Do NOT use string.GetHashCode — it is randomized per process.
    public static class StableHash
    {
        const uint FnvOffset = 2166136261;
        const uint FnvPrime = 16777619;

        public static uint OfString(string s)
        {
            uint hash = FnvOffset;
            if (s != null)
                for (int i = 0; i < s.Length; i++)
                {
                    unchecked { hash ^= s[i]; hash *= FnvPrime; }
                }
            return hash;
        }

        public static uint Combine(params int[] values)
        {
            uint hash = FnvOffset;
            for (int i = 0; i < values.Length; i++)
                unchecked
                {
                    uint v = (uint)values[i];
                    hash ^= v & 0xFF;         hash *= FnvPrime;
                    hash ^= (v >> 8) & 0xFF;  hash *= FnvPrime;
                    hash ^= (v >> 16) & 0xFF; hash *= FnvPrime;
                    hash ^= (v >> 24) & 0xFF; hash *= FnvPrime;
                }
            return hash;
        }

        /// Maps a hash to [minInclusive, maxInclusive].
        public static int RangeInclusive(uint hash, int minInclusive, int maxInclusive)
        {
            if (maxInclusive <= minInclusive) return minInclusive;
            uint span = (uint)(maxInclusive - minInclusive + 1);
            return minInclusive + (int)(hash % span);
        }
    }
}
