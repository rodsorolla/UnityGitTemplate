namespace Sorolla.Events
{
    /// <summary>
    /// In-memory per-run counter. Created by IEventService.BeginRunCollector,
    /// fed by game-side collectible hooks, and either committed at level-complete
    /// (IEventService.CommitRun) or discarded at level-fail/quit. NOT persisted.
    /// </summary>
    public sealed class EventCollector
    {
        public string EventId { get; }
        public int CollectedThisRun { get; private set; }

        public EventCollector(string eventId)
        {
            EventId = eventId;
        }

        /// <summary>Add to this run's counter. Negative amounts are ignored.</summary>
        public void Add(int amount)
        {
            if (amount <= 0) return;
            CollectedThisRun += amount;
        }

        /// <summary>Reset for the next run without reallocating.</summary>
        public void Reset() => CollectedThisRun = 0;
    }
}
