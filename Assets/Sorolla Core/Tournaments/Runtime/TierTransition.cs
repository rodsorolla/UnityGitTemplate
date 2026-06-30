namespace Sorolla.Tournaments
{
    public static class TierTransition
    {
        public static int Apply(int currentTierIndex, TournamentOutcome outcome, int tierCount)
        {
            int next = currentTierIndex;
            if (outcome == TournamentOutcome.Promoted) next = currentTierIndex + 1;
            else if (outcome == TournamentOutcome.Demoted) next = currentTierIndex - 1;
            if (next < 0) next = 0;
            if (tierCount > 0 && next > tierCount - 1) next = tierCount - 1;
            return next;
        }
    }
}
