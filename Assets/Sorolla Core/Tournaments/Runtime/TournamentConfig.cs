using System.Collections.Generic;
using UnityEngine;

namespace Sorolla.Tournaments
{
    [CreateAssetMenu(fileName = "TournamentConfig", menuName = "Sorolla/Tournaments/Tournament Config")]
    public class TournamentConfig : ScriptableObject
    {
        public List<TierDefinition> tiers = new List<TierDefinition>();
        public List<string> botNames = new List<string>();

        public TournamentConfigData ToData() => new TournamentConfigData(tiers, botNames);
    }
}
