namespace Sorolla.Tournaments
{
    /// <summary>Rank change to animate on the rank-reveal strip. Supplied by the caller
    /// (the game's end-screen wiring), so the widget carries no game-side persistence.</summary>
    public struct RankReveal
    {
        public int OldRank;
        public int NewRank;
        public bool Improved;
    }
}
