using System;

namespace Sorolla.Events.Tests.Helpers
{
    public sealed class FakeAuthoritativeTime : IAuthoritativeTime
    {
        private DateTime _now;
        private bool _rollback;

        public FakeAuthoritativeTime(DateTime startUtc)
        {
            if (startUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("FakeAuthoritativeTime requires UTC", nameof(startUtc));
            _now = startUtc;
        }

        public DateTime UtcNow => _now;
        public bool RollbackDetectedThisSession => _rollback;
        public event Action OnRollbackDetected;

        public void Advance(TimeSpan delta) => _now = _now.Add(delta);
        public void SetUtcNow(DateTime newUtc)
        {
            if (newUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("FakeAuthoritativeTime requires UTC", nameof(newUtc));
            _now = newUtc;
        }

        public void ObservePersisted(DateTime persistedUtc, TimeSpan grace)
        {
            if (persistedUtc - _now > grace && !_rollback)
            {
                _rollback = true;
                OnRollbackDetected?.Invoke();
            }
        }
    }
}
