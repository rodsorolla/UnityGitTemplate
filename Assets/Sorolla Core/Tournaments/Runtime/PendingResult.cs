namespace Sorolla.Tournaments
{
    [System.Serializable]
    public class PendingResult
    {
        public int WeekIndex;
        public int TierIndex;
        public int FinalRank;
        public TournamentOutcome Outcome;
        public bool Claimed;
    }
}
