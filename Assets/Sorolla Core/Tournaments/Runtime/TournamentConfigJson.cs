using System;
using System.Collections.Generic;
using Sorolla.Events;
using UnityEngine;

namespace Sorolla.Tournaments
{
    /// Parses an optional RemoteConfig JSON override into TournamentConfigData.
    /// The GAME assigns ActiveJsonProvider at boot (Core must not reference game code).
    public static class TournamentConfigJson
    {
        /// e.g. game sets: TournamentConfigJson.ActiveJsonProvider = () => RemoteConfig.TournamentConfigJson;
        public static Func<string> ActiveJsonProvider;

        [Serializable] class Dto { public List<TierDto> tiers; public List<string> botNames; }
        [Serializable] class TierDto
        {
            public string name; public string iconId; public int groupSize;
            public int botPaceMin; public int botPaceMax; public float promotePct; public float demotePct;
            public List<RewardDto> podium1; public List<RewardDto> podium2; public List<RewardDto> podium3;
        }
        [Serializable] class RewardDto { public string type; public string id; public int amount; }

        public static TournamentConfigData TryParseActive(out string error)
        {
            error = null;
            var json = ActiveJsonProvider?.Invoke();
            if (string.IsNullOrWhiteSpace(json)) return null;
            return Parse(json, out error);
        }

        public static TournamentConfigData Parse(string json, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(json)) return null;

            Dto dto;
            try { dto = JsonUtility.FromJson<Dto>(json); }
            catch (Exception ex) { error = ex.Message; return null; }

            if (dto == null || dto.tiers == null || dto.tiers.Count == 0)
            {
                error = "No tiers in tournament config json.";
                return null;
            }

            var tiers = new List<TierDefinition>(dto.tiers.Count);
            foreach (var t in dto.tiers)
            {
                if (t == null) continue;
                tiers.Add(new TierDefinition
                {
                    name = t.name ?? "Tier",
                    iconId = t.iconId ?? "",
                    groupSize = t.groupSize > 1 ? t.groupSize : 100,
                    botPaceMin = t.botPaceMin,
                    botPaceMax = t.botPaceMax,
                    promotePct = t.promotePct,
                    demotePct = t.demotePct,
                    podiumRank1 = ToRewards(t.podium1),
                    podiumRank2 = ToRewards(t.podium2),
                    podiumRank3 = ToRewards(t.podium3)
                });
            }

            var names = dto.botNames ?? new List<string>();
            return new TournamentConfigData(tiers, names);
        }

        static EventReward[] ToRewards(List<RewardDto> list)
        {
            if (list == null || list.Count == 0) return Array.Empty<EventReward>();
            var arr = new EventReward[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                arr[i] = new EventReward
                {
                    ItemType = r?.type ?? "",
                    ItemId = r?.id ?? "",
                    Amount = r?.amount ?? 0
                };
            }
            return arr;
        }
    }
}
