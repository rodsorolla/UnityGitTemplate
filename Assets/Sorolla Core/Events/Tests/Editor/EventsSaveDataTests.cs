using NUnit.Framework;

namespace Sorolla.Events.Tests
{
    public class EventsSaveDataTests
    {
        [Test]
        public void FindOrCreate_AddsWhenMissing()
        {
            var s = new EventsSaveData();
            var inst = s.FindOrCreate("evt_a", "2026-05-15T12:00:00Z");
            Assert.AreEqual("evt_a", inst.eventId);
            Assert.AreEqual(1, s.instances.Count);
        }

        [Test]
        public void FindOrCreate_ReturnsExisting()
        {
            var s = new EventsSaveData();
            var a = s.FindOrCreate("evt_a", "2026-05-15T12:00:00Z");
            a.progress = 50;
            var again = s.FindOrCreate("evt_a", "2026-05-16T12:00:00Z");
            Assert.AreSame(a, again);
            Assert.AreEqual(50, again.progress);
            Assert.AreEqual(1, s.instances.Count);
        }

        [Test]
        public void Find_ReturnsNullWhenMissing()
        {
            var s = new EventsSaveData();
            Assert.IsNull(s.Find("nope"));
        }

        [Test]
        public void Remove_DropsTheEntry()
        {
            var s = new EventsSaveData();
            s.FindOrCreate("evt_a", "2026-05-15T12:00:00Z");
            s.FindOrCreate("evt_b", "2026-05-15T12:00:00Z");
            Assert.IsTrue(s.Remove("evt_a"));
            Assert.IsNull(s.Find("evt_a"));
            Assert.IsNotNull(s.Find("evt_b"));
        }

        [Test]
        public void Version_IsOne()
        {
            Assert.AreEqual(1, new EventsSaveData().Version);
        }
    }
}
