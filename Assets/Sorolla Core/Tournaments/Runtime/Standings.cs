using System.Collections.Generic;

namespace Sorolla.Tournaments
{
    /// Computes the player's rank among player+bots and the promote/demote bands.
    /// Tiebreak: on equal trophies the player ranks ABOVE bots.
    public static class Standings
    {
        public struct Result
        {
            public int PlayerRank;     // 1-based
            public int Group;
            public int PromoteCount;
            public int DemoteCount;
            public bool PlayerIsPodium;
            public TournamentOutcome PlayerOutcome;
        }

        public static Result Compute(int playerTrophies, IReadOnlyList<int> botTrophies,
            float promotePct, float demotePct)
        {
            int group = (botTrophies?.Count ?? 0) + 1;

            int ahead = 0;
            if (botTrophies != null)
                for (int i = 0; i < botTrophies.Count; i++)
                    if (botTrophies[i] > playerTrophies) ahead++;
            int playerRank = ahead + 1;

            // Round the product first: (double)0.20f is ~0.20000000298, so 100*it = 20.0000003,
            // which would otherwise Ceiling to 21. Rounding to 6 dp removes the float error.
            int promote = (int)System.Math.Ceiling(System.Math.Round(group * (double)promotePct, 6));
            int demote = (int)System.Math.Floor(System.Math.Round(group * (double)demotePct, 6));
            if (promote < 0) promote = 0;
            if (demote < 0) demote = 0;
            if (promote + demote > group) demote = group - promote;

            TournamentOutcome outcome;
            if (playerRank <= promote) outcome = TournamentOutcome.Promoted;
            else if (playerRank > group - demote) outcome = TournamentOutcome.Demoted;
            else outcome = TournamentOutcome.Stayed;

            return new Result
            {
                PlayerRank = playerRank,
                Group = group,
                PromoteCount = promote,
                DemoteCount = demote,
                PlayerIsPodium = playerRank <= 3,
                PlayerOutcome = outcome
            };
        }
    }
}
