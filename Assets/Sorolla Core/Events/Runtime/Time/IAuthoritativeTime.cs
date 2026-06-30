using System;

namespace Sorolla.Events
{
    /// <summary>
    /// Time source for the events module. Differs from the generic
    /// <see cref="Sorolla.ITimeSource"/> by adding rollback detection — the
    /// events save file persists the last observed UTC and any subsequent
    /// reading that's meaningfully earlier raises <see cref="OnRollbackDetected"/>.
    /// </summary>
    public interface IAuthoritativeTime
    {
        DateTime UtcNow { get; }
        bool RollbackDetectedThisSession { get; }
        event Action OnRollbackDetected;

        /// <summary>
        /// Observe a previously-persisted UTC. Implementations decide whether
        /// to set <see cref="RollbackDetectedThisSession"/>. Called by
        /// EventManager on load.
        /// </summary>
        void ObservePersisted(DateTime persistedUtc, TimeSpan graceTolerance);
    }
}
