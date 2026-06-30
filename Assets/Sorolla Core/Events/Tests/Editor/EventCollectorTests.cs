using NUnit.Framework;

namespace Sorolla.Events.Tests
{
    public class EventCollectorTests
    {
        [Test]
        public void Add_AccumulatesPositiveAmounts()
        {
            var c = new EventCollector("evt_a");
            c.Add(3);
            c.Add(5);
            Assert.AreEqual(8, c.CollectedThisRun);
        }

        [Test]
        public void Add_IgnoresZeroAndNegative()
        {
            var c = new EventCollector("evt_a");
            c.Add(0);
            c.Add(-7);
            Assert.AreEqual(0, c.CollectedThisRun);
        }

        [Test]
        public void Reset_ClearsCount()
        {
            var c = new EventCollector("evt_a");
            c.Add(10);
            c.Reset();
            Assert.AreEqual(0, c.CollectedThisRun);
        }

        [Test]
        public void EventId_IsExposed()
        {
            var c = new EventCollector("evt_a");
            Assert.AreEqual("evt_a", c.EventId);
        }
    }
}
