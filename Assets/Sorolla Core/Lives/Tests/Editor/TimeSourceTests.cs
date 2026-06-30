using System;
using NUnit.Framework;
using Sorolla;
using Sorolla.Lives.Tests.Helpers;

namespace Sorolla.Lives.Tests
{
    public class TimeSourceTests
    {
        [Test]
        public void FakeTimeSource_Advance_AddsToCurrentUtcNow()
        {
            var clock = new FakeTimeSource(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            clock.Advance(TimeSpan.FromMinutes(30));
            Assert.That(clock.UtcNow, Is.EqualTo(new DateTime(2026, 1, 1, 0, 30, 0, DateTimeKind.Utc)));
        }

        [Test]
        public void FakeTimeSource_SetUtcNow_OverwritesTime()
        {
            var clock = new FakeTimeSource(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var target = new DateTime(2020, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            clock.SetUtcNow(target);
            Assert.That(clock.UtcNow, Is.EqualTo(target));
        }

        [Test]
        public void FakeTimeSource_NonUtcCtor_Throws()
        {
            var localTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);
            Assert.Throws<ArgumentException>(() => new FakeTimeSource(localTime));
        }

        [Test]
        public void FakeTimeSource_SetUtcNow_NonUtc_Throws()
        {
            var clock = new FakeTimeSource(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var localTime = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Local);
            Assert.Throws<ArgumentException>(() => clock.SetUtcNow(localTime));
        }

        [Test]
        public void SystemTimeSource_UtcNow_IsUtcKind()
        {
            Assert.That(SystemTimeSource.Instance.UtcNow.Kind, Is.EqualTo(DateTimeKind.Utc));
        }
    }
}
