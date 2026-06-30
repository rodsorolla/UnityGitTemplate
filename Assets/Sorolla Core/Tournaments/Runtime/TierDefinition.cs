using System;
using Sorolla.Events;

namespace Sorolla.Tournaments
{
    [Serializable]
    public class TierDefinition
    {
        public string name = "Tier";
        public string iconId = "";
        public int groupSize = 100;
        public int botPaceMin = 5;
        public int botPaceMax = 40;
        public float promotePct = 0.20f;
        public float demotePct = 0.20f;

        public EventReward[] podiumRank1 = Array.Empty<EventReward>();
        public EventReward[] podiumRank2 = Array.Empty<EventReward>();
        public EventReward[] podiumRank3 = Array.Empty<EventReward>();

        public EventReward[] PodiumReward(int rank)
        {
            switch (rank)
            {
                case 1: return podiumRank1;
                case 2: return podiumRank2;
                case 3: return podiumRank3;
                default: return Array.Empty<EventReward>();
            }
        }
    }
}
