using NUnit.Framework;
using Sorolla;
using Sorolla.Events.Tests.Helpers;

namespace Sorolla.Events.Tests
{
    public class EventConfigKeysTests
    {
        [SetUp] public void Setup() => ServiceLocator.Reset();
        [TearDown] public void Teardown() => ServiceLocator.Reset();

        [Test]
        public void Defaults_WhenNoProvider_AreSpecValues()
        {
            Assert.IsTrue(EventConfigKeys.Enabled);
            Assert.AreEqual(12, EventConfigKeys.DefaultUnlockLevelValue);
            Assert.AreEqual(60, EventConfigKeys.ClockRollbackGraceSeconds);
        }

        [Test]
        public void Provider_OverridesValues()
        {
            var fake = new FakeRemoteConfigProvider();
            fake.Bools[EventConfigKeys.KeyEnabled] = false;
            fake.Ints[EventConfigKeys.KeyDefaultUnlockLevel] = 25;
            fake.Ints[EventConfigKeys.KeyClockRollbackGraceSeconds] = 120;
            ServiceLocator.Instance.Register<IRemoteConfigProvider>(fake);

            Assert.IsFalse(EventConfigKeys.Enabled);
            Assert.AreEqual(25, EventConfigKeys.DefaultUnlockLevelValue);
            Assert.AreEqual(120, EventConfigKeys.ClockRollbackGraceSeconds);
        }
    }
}
