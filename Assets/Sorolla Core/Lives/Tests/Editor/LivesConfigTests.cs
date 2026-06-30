using NUnit.Framework;
using Sorolla;
using Sorolla.Lives;
using Sorolla.Lives.Tests.Helpers;

namespace Sorolla.Lives.Tests
{
    public class LivesConfigTests
    {
        [SetUp]
        public void Setup() => ServiceLocator.Reset();

        [TearDown]
        public void Teardown() => ServiceLocator.Reset();

        [Test]
        public void Defaults_WhenNoProviderRegistered_AreSpecValues()
        {
            Assert.That(LivesConfig.MaxLives, Is.EqualTo(5));
            Assert.That(LivesConfig.RegenIntervalSeconds, Is.EqualTo(1800));
            Assert.That(LivesConfig.LivesSystemMinLevel, Is.EqualTo(5));
            Assert.That(LivesConfig.BoosterDefaultDurationSeconds, Is.EqualTo(1800));
        }

        [Test]
        public void CustomProvider_OverridesValues()
        {
            var fake = new FakeRemoteConfigProvider();
            fake.Ints["lives_max"] = 3;
            fake.Ints["lives_regen_interval_seconds"] = 600;
            fake.Ints["lives_system_min_level"] = 1;
            fake.Ints["lives_booster_default_duration_seconds"] = 60;
            ServiceLocator.Instance.Register<IRemoteConfigProvider>(fake);

            Assert.That(LivesConfig.MaxLives, Is.EqualTo(3));
            Assert.That(LivesConfig.RegenIntervalSeconds, Is.EqualTo(600));
            Assert.That(LivesConfig.LivesSystemMinLevel, Is.EqualTo(1));
            Assert.That(LivesConfig.BoosterDefaultDurationSeconds, Is.EqualTo(60));
        }
    }
}
