using System;
using Sorolla;

namespace Sorolla.Lives.Tests.Helpers
{
    /// <summary>
    /// Deterministic ITimeSource for unit tests. Advance manually with Advance() or SetUtcNow().
    /// </summary>
    public sealed class FakeTimeSource : ITimeSource
    {
        private DateTime _now;

        public FakeTimeSource(DateTime startUtc)
        {
            if (startUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("FakeTimeSource requires UTC time", nameof(startUtc));
            _now = startUtc;
        }

        public DateTime UtcNow => _now;

        public void Advance(TimeSpan delta) => _now = _now.Add(delta);
        public void SetUtcNow(DateTime newUtc)
        {
            if (newUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("FakeTimeSource requires UTC time", nameof(newUtc));
            _now = newUtc;
        }
    }
}
