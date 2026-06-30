using System;

namespace Sorolla.Events
{
    /// <summary>
    /// Public facade for the events module. Registered with ServiceLocator by EventManager.
    /// </summary>
    public interface IEventService
    {
        // ---- State ----
        EventDefinition ActiveEvent { get; }
        EventState GetState(string eventId);
        EventInstanceProgress GetProgress(string eventId);

        /// <summary>
        /// Sets the progress counter for <paramref name="eventId"/>. Creates the
        /// underlying <see cref="EventInstanceProgress"/> row if it doesn't exist yet.
        /// Marks the save dirty (persisted at the next checkpoint: pause, quit,
        /// commit, etc.) and fires <see cref="OnProgressChanged"/> with the delta.
        /// No-op if <paramref name="eventId"/> is null/empty or the new value equals
        /// the current value.
        /// </summary>
        void SetProgress(string eventId, int newProgress);

        bool IsUnlocked(int progressiveLevelIndex);
        TimeSpan TimeUntilActiveEnds { get; }
        TimeSpan TimeUntilNextEventStarts { get; }
        bool LastClockRollbackDetected { get; }

        // ---- Run lifecycle ----
        /// <summary>Returns null when no event is active.</summary>
        EventCollector BeginRunCollector();
        /// <summary>Commit a completed run. Null or empty collectors are no-ops.</summary>
        void CommitRun(EventCollector collector, EventCommitContext ctx = null);

        // ---- Reward claiming (reserved for future manual UX; no-op in v1) ----
        bool TryClaimStep(string eventId, int stepIndex);
        bool TryClaimGrandPrize(string eventId);

        // ---- Deferred UI animation ----
        /// <summary>
        /// Cumulative delta of committed runs the player hasn't seen animated yet.
        /// The main-menu tile reads this when the home screen becomes visible,
        /// plays the items-fly / bar-advance / reward-pop animation, then calls
        /// <see cref="ConsumePendingHomeAnimation"/> to clear it.
        /// </summary>
        int PendingHomeAnimationDelta { get; }
        void ConsumePendingHomeAnimation();

        // ---- Events ----
        event Action<EventDefinition> OnActiveEventStarted;
        event Action<EventDefinition, EventEndReason> OnActiveEventEnded;
        event Action<string /*eventId*/, int /*newProgress*/, int /*delta*/> OnProgressChanged;
        event Action<string /*eventId*/, int /*stepIndex*/> OnStepClaimed;
        event Action<string /*eventId*/> OnGrandPrizeClaimed;
        event Action OnClockRollbackDetected;
    }
}
