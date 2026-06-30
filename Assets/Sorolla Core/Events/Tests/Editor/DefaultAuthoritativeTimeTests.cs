using System;
using NUnit.Framework;

namespace Sorolla.Events.Tests
{
    public class DefaultAuthoritativeTimeTests
    {
        private sealed class StubClock : Sorolla.ITimeSource
        {
            public DateTime UtcNow { get; set; }
        }

        [Test]
        public void Rollback_NotFlagged_WhenPersistedIsInPast()
        {
            var clock = new StubClock { UtcNow = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc) };
            var t = new DefaultAuthoritativeTime(clock);
            t.ObservePersisted(new DateTime(2026, 5, 15, 11, 0, 0, DateTimeKind.Utc), TimeSpan.FromMinutes(1));
            Assert.IsFalse(t.RollbackDetectedThisSession);
        }

        [Test]
        public void Rollback_NotFlagged_WhenInsideGrace()
        {
            var clock = new StubClock { UtcNow = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc) };
            var t = new DefaultAuthoritativeTime(clock);
            t.ObservePersisted(new DateTime(2026, 5, 15, 12, 0, 30, DateTimeKind.Utc), TimeSpan.FromSeconds(60));
            Assert.IsFalse(t.RollbackDetectedThisSession);
        }

        [Test]
        public void Rollback_Flagged_WhenPersistedFutureExceedsGrace()
        {
            var clock = new StubClock { UtcNow = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc) };
            var t = new DefaultAuthoritativeTime(clock);
            var fired = false;
            t.OnRollbackDetected += () => fired = true;
            t.ObservePersisted(new DateTime(2026, 5, 15, 12, 10, 0, DateTimeKind.Utc), TimeSpan.FromSeconds(60));
            Assert.IsTrue(t.RollbackDetectedThisSession);
            Assert.IsTrue(fired);
        }

        [Test]
        public void Rollback_FiresOnlyOnce()
        {
            var clock = new StubClock { UtcNow = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc) };
            var t = new DefaultAuthoritativeTime(clock);
            int fireCount = 0;
            t.OnRollbackDetected += () => fireCount++;
            t.ObservePersisted(new DateTime(2026, 5, 15, 12, 10, 0, DateTimeKind.Utc), TimeSpan.FromSeconds(60));
            t.ObservePersisted(new DateTime(2026, 5, 15, 12, 20, 0, DateTimeKind.Utc), TimeSpan.FromSeconds(60));
            Assert.AreEqual(1, fireCount);
        }
    }
}
