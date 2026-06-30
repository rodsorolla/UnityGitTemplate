namespace Sorolla.Tournaments
{
    public enum RolloverAction { Hold = 0, Finalize = 1, NoOpAdvance = 2 }

    /// Decides what happens when the service observes the current week vs the active week.
    public static class RolloverPolicy
    {
        public static RolloverAction Decide(int activeWeek, int currentWeek, int playerTrophies, bool rollbackDetected)
        {
            if (rollbackDetected) return RolloverAction.Hold;
            if (currentWeek <= activeWeek) return RolloverAction.Hold;          // same or backwards week
            return playerTrophies >= 1 ? RolloverAction.Finalize : RolloverAction.NoOpAdvance;
        }
    }
}
