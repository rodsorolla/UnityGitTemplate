using NUnit.Framework;
using Sorolla;

namespace Sorolla.Lives.Tests
{
    public class IRemoteConfigProviderTests
    {
        [Test]
        public void DefaultProvider_ReturnsCallerSuppliedDefaults()
        {
            var rc = DefaultRemoteConfigProvider.Instance;

            Assert.That(rc.GetInt("any_key", 42), Is.EqualTo(42));
            Assert.That(rc.GetLong("any_key", 9001L), Is.EqualTo(9001L));
            Assert.That(rc.GetFloat("any_key", 1.5f), Is.EqualTo(1.5f));
            Assert.That(rc.GetBool("any_key", true), Is.True);
            Assert.That(rc.GetString("any_key", "fallback"), Is.EqualTo("fallback"));
        }

    }
}
