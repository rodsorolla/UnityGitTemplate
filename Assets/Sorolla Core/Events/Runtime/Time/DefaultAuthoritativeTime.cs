using System;

namespace Sorolla.Events
{
    /// <summary>
    /// Device-clock implementation. Wraps a <see cref="Sorolla.ITimeSource"/>
    /// (defaults to <see cref="Sorolla.SystemTimeSource"/>). Rollback detection
    /// follows the LivesManager pattern: if a persisted lastSeen UTC is more
    /// than the grace window in the future of the current reading, flag it.
    /// </summary>
    public sealed class DefaultAuthoritativeTime : IAuthoritativeTime
    {
        private readonly Sorolla.ITimeSource _clock;
        private bool _rollback;

        public DefaultAuthoritativeTime(Sorolla.ITimeSource clock = null)
        {
            _clock = clock ?? Sorolla.SystemTimeSource.Instance;
        }

        public DateTime UtcNow => _clock.UtcNow;
        public bool RollbackDetectedThisSession => _rollback;
        public event Action OnRollbackDetected;

        public void ObservePersisted(DateTime persistedUtc, TimeSpan graceTolerance)
        {
            if (persistedUtc.Kind != DateTimeKind.Utc) return;
            var now = _clock.UtcNow;
            if (persistedUtc - now > graceTolerance && !_rollback)
            {
                _rollback = true;
                OnRollbackDetected?.Invoke();
            }
        }
    }
}
