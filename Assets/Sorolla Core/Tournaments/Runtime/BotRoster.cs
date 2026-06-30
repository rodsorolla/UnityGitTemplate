using System.Collections.Generic;
using Sorolla.Profile;

namespace Sorolla.Tournaments
{
    /// Deterministic per-(tier, week) bot roster. Same seed -> identical roster.
    public static class BotRoster
    {
        public static List<Bot> Build(int tierIndex, int weekIndex, TierDefinition tier,
            ProfileCatalog catalog, IReadOnlyList<string> botNames)
        {
            var bots = new List<Bot>();
            if (tier == null) return bots;

            int count = tier.groupSize - 1;
            if (count < 0) count = 0;

            uint seed = StableHash.Combine(tierIndex, weekIndex);
            int nameCount = botNames?.Count ?? 0;
            int avatarCount = catalog != null ? catalog.avatars.Count : 0;
            int flagCount = catalog != null ? catalog.flags.Count : 0;

            for (int i = 0; i < count; i++)
            {
                uint hName = StableHash.Combine((int)seed, i, 1);
                uint hAvatar = StableHash.Combine((int)seed, i, 2);
                uint hFlag = StableHash.Combine((int)seed, i, 3);
                uint hTarget = StableHash.Combine((int)seed, i, 4);

                bots.Add(new Bot
                {
                    id = i,
                    displayName = nameCount > 0 ? botNames[(int)(hName % (uint)nameCount)] : "Player" + i,
                    avatarId = avatarCount > 0 ? catalog.avatars[(int)(hAvatar % (uint)avatarCount)].id : null,
                    countryCode = flagCount > 0 ? catalog.flags[(int)(hFlag % (uint)flagCount)].countryCode : null,
                    weeklyTarget = StableHash.RangeInclusive(hTarget, tier.botPaceMin, tier.botPaceMax)
                });
            }
            return bots;
        }
    }
}
