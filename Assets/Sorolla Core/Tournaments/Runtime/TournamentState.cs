using Sorolla.PersistentData;

namespace Sorolla.Tournaments
{
    [System.Serializable]
    public class TournamentState : ISaveData
    {
        public int Version => 1;

        public int CurrentTierIndex;
        public int ActiveWeekIndex;
        public int PlayerTrophies;
        public bool HasJoined;                  // true once the player has joined (trophies reset to 0 at unlock)
        public long LastSeenUtcTicks;          // last UTC tick we persisted; backward-clock guard
        public PendingResult PendingResult;    // null when nothing to claim
    }
}
