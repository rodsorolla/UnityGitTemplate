using System.Collections.Generic;

namespace Sorolla.Tournaments
{
    /// Plain config the service consumes — produced by the SO or by the RC JSON parser.
    public class TournamentConfigData
    {
        public IReadOnlyList<TierDefinition> Tiers { get; }
        public IReadOnlyList<string> BotNames { get; }

        public TournamentConfigData(IReadOnlyList<TierDefinition> tiers, IReadOnlyList<string> botNames)
        {
            Tiers = tiers ?? new List<TierDefinition>();
            BotNames = botNames ?? new List<string>();
        }

        public bool IsValid => Tiers.Count > 0 && BotNames.Count > 0;
    }
}
